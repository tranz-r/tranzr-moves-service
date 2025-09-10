using Mediator;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using TranzrMoves.Api.Dtos;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Quote.Save;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Services.EmailTemplates;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class CheckoutController(StripeClient stripeClient, 
    IConfiguration configuration, 
    ILogger<CheckoutController> logger,
    IQuoteRepository quoteRepository,
    IUserRepository userRepository,
    IMediator mediator,
    IAwsEmailService awsEmailService,
    ITemplateService templateService) : ApiControllerBase
{
    [HttpPost("payment-sheet", Name = "CreateStripeIntent")]
    public async Task<ActionResult<PaymentSheetCreateResponse>> CreatePaymentSheet([FromBody] SaveQuoteRequest? saveQuoteRequest, CancellationToken ct)
    {
        // Use an existing Customer ID if this is a returning customer.
        logger.LogInformation("Creating payment sheet");
        
        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{saveQuoteRequest?.Customer?.Email}'",
        }, cancellationToken: ct);
        
        var customer = customerSearchResult.Data.FirstOrDefault();
        
        if (customer is null)
        { 
            logger.LogInformation("Customer does not exist. Creating customer with {email}",  saveQuoteRequest?.Customer?.Email);

            var splitAddress = saveQuoteRequest?.Customer?.BillingAddress?.Line1.Split(',');
            var city = splitAddress?[^2].Trim();
            var customerOptions = new CustomerCreateOptions
            {
                Email = saveQuoteRequest?.Customer?.Email,
                Name = saveQuoteRequest?.Customer?.FullName,
                Address = new AddressOptions
                {
                    Line1 = splitAddress?.FirstOrDefault()?.Trim(),
                    Line2 = splitAddress?.Length == 4 ? splitAddress[2].Trim() : string.Empty,
                    City = city,
                    PostalCode = splitAddress?.LastOrDefault()?.Trim(),
                    Country = "United Kingdom"
                }
            };
            
            customer = await stripeClient.V1.Customers.CreateAsync(customerOptions, cancellationToken: ct);
        }
        
        var ephemeralKeyOptions = new EphemeralKeyCreateOptions
        {
            Customer = customer.Id,
            StripeVersion = "2025-06-30.basil",
        };
        
        var ephemeralKey = await stripeClient.V1.EphemeralKeys.CreateAsync(ephemeralKeyOptions, cancellationToken: ct);

        // Determine payment amount based on payment type
        long paymentAmount = 0;
        string description = "Your Tranzr Moves payment";
        
        switch (saveQuoteRequest?.Quote.Payment?.PaymentType)
        {
            case PaymentType.Full:
                paymentAmount = (long)Math.Round((decimal)(saveQuoteRequest.Quote.Pricing?.TotalCost * 100)!, 0, MidpointRounding.AwayFromZero);
                description = "Your Tranzr Moves payment - Full amount";
                break;
                
            case PaymentType.Deposit:
                // Calculate deposit amount from percentage for security
                var depositPercentage = 25m; // Default to 25% if not specified
                var calculatedDepositAmount = (decimal)saveQuoteRequest.Quote.Pricing?.TotalCost! * (depositPercentage / 100m);
                paymentAmount = (long)Math.Round(calculatedDepositAmount * 100, 0, MidpointRounding.AwayFromZero);
                
                // Log the calculation for audit
                logger.LogInformation("Deposit calculation: Total {Total} * {Percentage}% = {DepositAmount}", 
                    saveQuoteRequest.Quote.Pricing?.TotalCost, depositPercentage, calculatedDepositAmount);
                
                description = $"Your Tranzr Moves payment - {depositPercentage}% deposit";
                break;
                
            case PaymentType.Later:
                // For "pay later", we create a Setup Intent for saving payment methods
                paymentAmount = 0;
                description = "Your Tranzr Moves payment - Payment deferred";
                break;
        }

        // Handle different payment types
        if (saveQuoteRequest is { Quote.Payment.PaymentType: PaymentType.Later })
        {
            // Create Setup Intent for "pay later" option
            var setupIntentOptions = new SetupIntentCreateOptions
            {
                Customer = customer.Id,
                Description = description,
                Usage = "off_session", // Enable automatic charging later
                
                PaymentMethodTypes = ["card","link"],
                
                // Add metadata for payment tracking
                Metadata = new Dictionary<string, string>
                {
                    { nameof(PaymentMetadata.PaymentType), saveQuoteRequest.Quote.Payment.PaymentType.ToString() },
                    { nameof(PaymentMetadata.TotalCost), saveQuoteRequest.Quote.Pricing?.TotalCost?.ToString("F2")! },
                    { nameof(PaymentMetadata.DepositPercentage), saveQuoteRequest.Quote.Payment.DepositPercentage?.ToString() ?? "0" },
                    { nameof(PaymentMetadata.DueDate), saveQuoteRequest.Quote.Payment.DueDate?.ToString("yyyy-MM-dd") ?? "" },
                    { nameof(PaymentMetadata.QuoteReference), saveQuoteRequest.Quote.QuoteReference ?? "" },
                    { nameof(PaymentMetadata.QuoteId), saveQuoteRequest.Quote.Id.ToString() },
                    { nameof(PaymentMetadata.PaymentDueDate), saveQuoteRequest.Quote.Payment.DueDate?.ToString("yyyy-MM-dd") ?? "" }
                }
            };
            
            var setupIntent = await stripeClient.V1.SetupIntents.CreateAsync(setupIntentOptions, cancellationToken: ct);
            
            logger.LogInformation("Setup intent created for {PaymentType} to save payment method", saveQuoteRequest.Quote.Payment.PaymentType);

            saveQuoteRequest.Quote.Payment.PaymentIntentId = setupIntent.Id;
            saveQuoteRequest.Quote.Payment.DueDate = saveQuoteRequest.Quote.Schedule!.DateISO!.Value.AddHours(-72); // Due 72 hours before move
            
            _ = await UpdateQuoteAsync(saveQuoteRequest, ct);
            
            return new PaymentSheetCreateResponse
            {
                PaymentIntent = setupIntent.ClientSecret,
                PaymentIntentId = setupIntent.Id,
                EphemeralKey = ephemeralKey.Secret,
                Customer = customer.Id,
                PublishableKey = StripeConfiguration.ClientId
            };
        }

        // Create PaymentIntent for Full and Deposit payments
        var paymentIntentOptions = new PaymentIntentCreateOptions
        {
            Amount = paymentAmount,
            Currency = "gbp",
            Customer = customer.Id,
            Description = description,
            ReceiptEmail = saveQuoteRequest?.Customer?.Email,
            
            // Add metadata for payment tracking
            Metadata = new Dictionary<string, string>
            {
                { nameof(PaymentMetadata.PaymentType), saveQuoteRequest!.Quote.Payment?.PaymentType.ToString()! },
                { nameof(PaymentMetadata.TotalCost), saveQuoteRequest.Quote.Pricing?.TotalCost?.ToString("F2")! },
                { nameof(PaymentMetadata.DepositPercentage), saveQuoteRequest.Quote.Payment!.DepositPercentage?.ToString() ?? "0" },
                { nameof(PaymentMetadata.DueDate), saveQuoteRequest.Quote.Payment.DueDate?.ToString("yyyy-MM-dd") ?? "" },
                { nameof(PaymentMetadata.QuoteReference), saveQuoteRequest.Quote.QuoteReference ?? "" },
                { nameof(PaymentMetadata.QuoteId), saveQuoteRequest.Quote.Id.ToString() }
            }
        };
            
        // Set up future usage for deposits
        if (saveQuoteRequest is { Quote.Payment.PaymentType: PaymentType.Full })
        {
            // For full payment, use automatic payment methods
            paymentIntentOptions.AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "always"
            };
        }
        else
        {
            // For deposit payments, specify payment method types and enable future usage
            paymentIntentOptions.PaymentMethodTypes = ["card", "link"];
            paymentIntentOptions.SetupFutureUsage = "off_session"; // Enable automatic charging later
            saveQuoteRequest.Quote.Payment.DueDate = saveQuoteRequest.Quote.Schedule!.DateISO;
            
            logger.LogInformation("PaymentIntent created with setup_future_usage for {PaymentType}", 
                saveQuoteRequest.Quote.Payment.PaymentType);
        }
            
        var paymentIntent = await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions, cancellationToken: ct);
            
        logger.LogInformation("Payment intent created for {PaymentType} with amount {Amount}", 
            saveQuoteRequest.Quote.Payment.PaymentType, paymentAmount);
        
        saveQuoteRequest.Quote.Payment.PaymentIntentId = paymentIntent.Id;
        
        await UpdateQuoteAsync(saveQuoteRequest, ct);
            
        return new PaymentSheetCreateResponse
        {
            PaymentIntent = paymentIntent.ClientSecret,
            PaymentIntentId = paymentIntent.Id,
            EphemeralKey = ephemeralKey.Secret,
            Customer = customer.Id,
            PublishableKey = StripeConfiguration.ClientId
        };
    }
    
    private async Task<SaveQuoteResponse> UpdateQuoteAsync(SaveQuoteRequest saveQuoteRequest, CancellationToken ct = default)
    {
        var command = new SaveQuoteCommand(saveQuoteRequest.Quote, saveQuoteRequest.Customer, saveQuoteRequest.ETag);
        var result = await mediator.Send(command, ct);

        return result.Value;
    }
    
    [HttpGet("payment-intent", Name = "GetPaymentIntent")]
    public async Task<ActionResult<PaymentIntentResponse>> GetPaymentIntent([FromQuery] string paymentIntentId)
    {
        // Check if this is a Setup Intent (starts with 'seti_') or Payment Intent (starts with 'pi_')
        if (paymentIntentId.StartsWith("seti_"))
        {
            // This is a Setup Intent (from "Pay later" option)
            var setupIntent = await stripeClient.V1.SetupIntents.GetAsync(paymentIntentId);
            
            return new PaymentIntentResponse
            {
                ClientSecret = setupIntent.ClientSecret,
                PaymentIntentId = setupIntent.Id,
                PublishableKey = StripeConfiguration.ClientId,
            };
        }

        // This is a Payment Intent (from "Pay in full" or "Pay deposit" options)
        var paymentIntent = await stripeClient.V1.PaymentIntents.GetAsync(paymentIntentId);
 
        return new PaymentIntentResponse
        {
            ClientSecret = paymentIntent.ClientSecret,
            PaymentIntentId = paymentIntent.Id,
            PublishableKey = StripeConfiguration.ClientId,
        };
    }
    
    [HttpPost("create-future-payment", Name = "CreateFuturePayment")]
    public async Task<ActionResult<PaymentIntentResponse>> CreateFuturePayment([FromBody] FuturePaymentRequest request)
    {
        // This endpoint handles creating PaymentIntents for the remaining balance
        // after a deposit has been paid, using saved payment methods
        
        try
        {
            logger.LogInformation("Creating future payment for customer {CustomerId} with remaining amount {Amount}", 
                request.CustomerId, request.RemainingAmount);
            
            // Enhanced validation: Ensure remaining amount matches expected calculation
            var expectedRemainingAmount = request.OriginalTotalCost - request.OriginalDepositAmount;
            var amountDifference = Math.Abs(request.RemainingAmount - expectedRemainingAmount);
            
            // Validate that original values are reasonable
            if (request.OriginalTotalCost <= 0)
            {
                logger.LogWarning("Invalid original total cost for customer {CustomerId}: {Amount}", 
                    request.CustomerId, request.OriginalTotalCost);
                return BadRequest("Original total cost must be greater than zero.");
            }
            
            if (request.OriginalDepositAmount < 0 || request.OriginalDepositAmount >= request.OriginalTotalCost)
            {
                logger.LogWarning("Invalid deposit amount for customer {CustomerId}: {Deposit} out of {Total}", 
                    request.CustomerId, request.OriginalDepositAmount, request.OriginalTotalCost);
                return BadRequest("Deposit amount must be between 0 and the original total cost.");
            }
            
            if (amountDifference > 0.01m) // Allow for small rounding differences (1 penny)
            {
                logger.LogWarning("Invalid remaining amount for customer {CustomerId}. Expected: {Expected}, Provided: {Provided}, Difference: {Difference}", 
                    request.CustomerId, expectedRemainingAmount, request.RemainingAmount, amountDifference);
                return BadRequest($"Invalid remaining amount. Expected: £{expectedRemainingAmount:F2}, Provided: £{request.RemainingAmount:F2}. " +
                    $"The remaining amount should be the original total (£{request.OriginalTotalCost:F2}) minus the deposit paid (£{request.OriginalDepositAmount:F2}).");
            }
            
            logger.LogInformation("Amount validation passed for customer {CustomerId}. Expected: {Expected}, Provided: {Provided}", 
                request.CustomerId, expectedRemainingAmount, request.RemainingAmount);
            
            var customer = await stripeClient.V1.Customers.GetAsync(request.CustomerId);
            if (customer == null)
            {
                logger.LogWarning("Customer not found for future payment: {CustomerId}", request.CustomerId);
                return BadRequest("Customer not found");
            }

            // Get customer's payment methods (from previous Setup Intents)
            var paymentMethods = await stripeClient.V1.PaymentMethods.ListAsync(new PaymentMethodListOptions
            {
                Customer = request.CustomerId,
                Type = "card" // Focus on card payments for now
            });

            if (!paymentMethods.Data.Any())
            {
                logger.LogWarning("No saved payment methods found for customer {CustomerId}", request.CustomerId);
                return BadRequest("No saved payment methods found. Customer must complete initial payment setup first.");
            }

            // Use the first available payment method
            var paymentMethod = paymentMethods.Data.First();
            logger.LogInformation("Using saved payment method {PaymentMethodId} for customer {CustomerId}", 
                paymentMethod.Id, request.CustomerId);

            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = (long)Math.Round(request.RemainingAmount * 100, 0, MidpointRounding.AwayFromZero),
                Currency = "gbp",
                Customer = request.CustomerId,
                PaymentMethod = paymentMethod.Id, // Use the saved payment method
                Description = $"Tranzr Moves - Remaining balance payment",
                ReceiptEmail = request.CustomerEmail,
                Confirm = true, // Automatically confirm the payment
                OffSession = true, // This is an off-session payment (automatic)
                Metadata = new Dictionary<string, string>
                {
                    { nameof(PaymentMetadata.PaymentType), nameof(PaymentType.Balance) },
                    { nameof(PaymentMetadata.QuoteReference), request.QuoteReference },
                    { "deposit_paid", "true" }, //TODO: Remove maybe
                    { "customer_name", request.CustomerName }, //TODO: Remove maybe
                    { nameof(PaymentMetadata.PaymentMethodId), paymentMethod.Id },
                    { nameof(PaymentMetadata.TotalCost), request.OriginalTotalCost.ToString("F2") },
                    { nameof(PaymentMetadata.DepositAmount), request.OriginalDepositAmount.ToString("F2") },
                    { nameof(PaymentMetadata.BalanceAmount), expectedRemainingAmount.ToString("F2") },
                    { "validation_passed", "true" }
                }
            };

            var paymentIntent = await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions);

            logger.LogInformation("Future payment intent created {PaymentIntentId} for remaining amount {Amount} using saved payment method", 
                paymentIntent.Id, request.RemainingAmount);

            return new PaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                PublishableKey = StripeConfiguration.ClientId,
            };
        }
        catch (StripeException ex)
        {
            if (ex.StripeError.Type == "card_error" && ex.StripeError.Code == "authentication_required")
            {
                logger.LogWarning("Payment requires authentication for customer {CustomerId}. Customer needs to complete 3D Secure.", 
                    request.CustomerId);
                return BadRequest("Payment requires additional authentication. Please contact customer to complete payment.");
            }
            
            logger.LogError(ex, "Stripe error creating future payment for customer {CustomerId}: {ErrorCode} - {ErrorMessage}", 
                request.CustomerId, ex.StripeError.Code, ex.StripeError.Message);
            return BadRequest($"Payment failed: {ex.StripeError.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create future payment for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, "Failed to create future payment");
        }
    }
    
    [HttpPost("webhook")]
    public async Task<IActionResult> ProcessPaymentWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        string? endpointSecret = configuration["TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET"];
        
        try
        {
            var stripeEvent = EventUtility.ParseEvent(json);
            var signatureHeader = Request.Headers["Stripe-Signature"];

            stripeEvent = EventUtility.ConstructEvent(json,
                signatureHeader, endpointSecret);

            // If on SDK version < 46, use class Events instead of EventTypes
            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                logger.LogInformation("A successful payment for {paymentAmount} GBP was made.", paymentIntent.Amount);
                
                // Send order confirmation email
                await HandlePaymentIntentSucceeded(paymentIntent);
            }
            else if (stripeEvent.Type == EventTypes.SetupIntentSucceeded)
            {
                var setupIntent = stripeEvent.Data.Object as SetupIntent;
                logger.LogInformation("A successful setup intent was completed for customer {CustomerId}.", setupIntent.CustomerId);
                
                await HandleSetupIntentSucceeded(setupIntent);
            }
            else if (stripeEvent.Type == EventTypes.PaymentMethodAttached)
            {
                var paymentMethod = stripeEvent.Data.Object as PaymentMethod;
                // Then define and call a method to handle the successful attachment of a PaymentMethod.
                // handlePaymentMethodAttached(paymentMethod);
            }
            else
            {
                logger.LogWarning("Unhandled event type: {0}", stripeEvent.Type);
            }
            return Ok();
        }
        catch (StripeException e)
        {
            logger.LogError("Error: {0}", e.Message);
            return BadRequest();
        }
        catch (Exception e)
        {
            return StatusCode(500);
        }
    }
    
    private async Task HandlePaymentIntentSucceeded(PaymentIntent paymentIntent)
    {
        try
        {
            // Get customer details from Stripe
            var customer = await stripeClient.V1.Customers.GetAsync(paymentIntent.CustomerId);
            var user = await userRepository.GetUserByEmailAsync(customer.Email, CancellationToken.None);
            
            if (!string.IsNullOrEmpty(customer.Email))
            {
                var orderDate = DateTime.UtcNow;
                
                var hasPaymentType =
                    paymentIntent.Metadata.TryGetValue(nameof(PaymentMetadata.PaymentType), out var paymentType);
                
                if (!hasPaymentType)
                {
                    return;
                }
                
                // Get quote by QuoteReference from metadata if available
                var quoteReference = paymentIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.QuoteReference), "");
                
                if (string.IsNullOrEmpty(quoteReference))
                {
                    logger.LogWarning("No quote reference found in metadata for payment intent {PaymentIntentId}", paymentIntent.Id);
                    return;
                }
                
                // Retrieve the latest charge
                var charge = await stripeClient.V1.Charges.GetAsync(paymentIntent.LatestChargeId);
                
                // Retrieve the quote from your database
                var quote = await quoteRepository.GetQuoteByReferenceAsync(quoteReference, paymentIntent.Id);
                
                var userMapper = new UserMapper();
                var mapper = new QuoteMapper();
                var quoteDto = mapper.ToDto(quote);
                
                quoteDto.Payment!.ReceiptUrl = charge.ReceiptUrl;
                quoteDto.Payment.PaymentMethodId = paymentIntent.PaymentMethodId;
                
                if (paymentType == nameof(PaymentType.Deposit))
                {
                    // Send deposit confirmation email
                    logger.LogInformation("Sending deposit confirmation email for payment intent {PaymentIntentId}", paymentIntent.Id);
                    
                    quoteDto.Payment.Status = PaymentStatus.PartiallyPaid;
                    
                    var quoteDeposit = await UpdateQuoteAsync(new SaveQuoteRequest
                    {
                        Quote = quoteDto,
                        Customer = userMapper.ToDto(user!)
                    });
                    
                    var depositAmount = paymentIntent.Amount / 100.0m;
                    var totalCost = quoteDto.Pricing?.TotalCost ?? depositAmount;
                    var remainingAmount = totalCost - depositAmount;
                    
                    var templateData = new
                    {
                        customerName = quoteDeposit.Customer!.FullName,
                        depositAmount = depositAmount.ToString("N2"),
                        totalAmount = totalCost.ToString("N2"),
                        remainingAmount = remainingAmount.ToString("N2"),
                        quoteReference,
                        paymentDate = orderDate.ToString("dddd, MMMM dd, yyyy"),
                        paymentTime = orderDate.ToString("HH:mm") + " GMT",
                        currentYear = DateTime.UtcNow.Year
                    };
                    
                    var subject = $"Deposit Confirmation - #{quoteReference}";
                    var htmlEmail = templateService.GenerateEmail("deposit-confirmation.html", templateData);
                    var textEmail = templateService.GenerateEmail("deposit-confirmation.txt", templateData);

                    await awsEmailService.SendBookingConfirmationEmailAsync(subject, customer.Email, htmlEmail, textEmail);
                    
                    logger.LogInformation("Deposit confirmation email sent for payment intent {PaymentIntentId}", paymentIntent.Id);
                }
                else if (paymentType == nameof(PaymentType.Balance))
                {
                    // Send balance payment confirmation email
                    logger.LogInformation("Sending balance payment confirmation email for payment intent {PaymentIntentId}", paymentIntent.Id);
                    
                    quoteDto.Payment.Status = PaymentStatus.Paid;
                    await UpdateQuoteAsync(new SaveQuoteRequest
                    {
                        Quote = quoteDto,
                        Customer = userMapper.ToDto(user!)
                    });
                    
                    var balanceAmount = paymentIntent.Amount / 100.0m;
                    var totalCost = quoteDto.Pricing?.TotalCost ?? balanceAmount;
                    
                    var templateData = new
                    {
                        customerName = user?.FullName,
                        balanceAmount = balanceAmount.ToString("N2"),
                        totalAmount = totalCost.ToString("N2"),
                        quoteReference,
                        paymentDate = orderDate.ToString("dddd, MMMM dd, yyyy"),
                        paymentTime = orderDate.ToString("HH:mm") + " GMT",
                        currentYear = DateTime.UtcNow.Year
                    };
                    
                    var subject = $"Balance Payment Confirmation - #{quoteReference}";
                    var htmlEmail = templateService.GenerateEmail("balance-confirmation.html", templateData);
                    var textEmail = templateService.GenerateEmail("balance-confirmation.txt", templateData);
                    
                    await awsEmailService.SendBookingConfirmationEmailAsync(subject, customer.Email, htmlEmail, textEmail);
                    
                    logger.LogInformation("Balance payment confirmation email sent for payment intent {PaymentIntentId}", paymentIntent.Id);
                }
                else
                {
                    quoteDto.Payment.Status = PaymentStatus.Paid;
                    await UpdateQuoteAsync(new SaveQuoteRequest
                    {
                        Quote = quoteDto,
                        Customer = userMapper.ToDto(user!)
                    });
                    
                    var fullAmount = paymentIntent.Amount / 100.0m;
                    
                    var templateData = new
                    {
                        customerName = user?.FullName,
                        totalAmount = fullAmount.ToString("N2"),
                        quoteReference,
                        paymentDate = orderDate.ToString("dddd, MMMM dd, yyyy"),
                        paymentTime = orderDate.ToString("HH:mm") + " GMT",
                        currentYear = DateTime.UtcNow.Year
                    };
                    
                    var subject = $"Order Confirmation - #{quoteReference}";
                    var htmlEmail = templateService.GenerateEmail("full-payment-confirmation.html", templateData);
                    var textEmail = templateService.GenerateEmail("full-payment-confirmation.txt", templateData);
                    
                    await awsEmailService.SendBookingConfirmationEmailAsync(subject, customer.Email, htmlEmail, textEmail);
                    
                    logger.LogInformation("Order confirmation email sent for payment intent {PaymentIntentId}", paymentIntent.Id);
                }
                
            }
            else
            {
                logger.LogWarning("Could not send confirmation email - customer email not found for payment intent {PaymentIntentId}", paymentIntent.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email for payment intent {PaymentIntentId}", paymentIntent.Id);
            // Don't throw here to avoid failing the webhook
        }
    }
    
    private async Task HandleSetupIntentSucceeded(SetupIntent setupIntent)
    {
        try
        {
            // Get customer details from Stripe
            var customer = await stripeClient.V1.Customers.GetAsync(setupIntent.CustomerId);
            
            var user = await userRepository.GetUserByEmailAsync(customer.Email, CancellationToken.None);
            
            if (!string.IsNullOrEmpty(customer.Email))
            {
                var setupDate = DateTime.UtcNow;
                
                // Get quote by QuoteReference from metadata if available
                var quoteReference = setupIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.QuoteReference), "");
                var quoteId = setupIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.QuoteId), "");
                
                if (string.IsNullOrEmpty(quoteReference))
                {
                    logger.LogWarning("No quote reference found in metadata for payment intent {PaymentIntentId}", setupIntent.Id);
                    return;
                }
                
                // Retrieve the quote from your database
                var quote = await quoteRepository.GetQuoteByReferenceAsync(quoteReference, setupIntent.Id);
                
                var userMapper = new UserMapper();
                var mapper = new QuoteMapper();
                var quoteDto = mapper.ToDto(quote);
                
                quoteDto.Payment!.PaymentMethodId = setupIntent.PaymentMethodId;
                
                // Check payment type from metadata
                //var paymentType = setupIntent.Metadata.GetValueOrDefault("payment_type", "later");
                var totalCost = decimal.Parse(setupIntent.Metadata.GetValueOrDefault("total_cost", "0"));
                var dueDate = setupIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.PaymentDueDate), "");
                
                var hasPaymentType =
                    setupIntent.Metadata.TryGetValue(nameof(PaymentMetadata.PaymentType), out var paymentType);
                
                if (!hasPaymentType)
                {
                    return;
                }
                
                if (paymentType == nameof(PaymentType.Later))
                {
                    // Send setup confirmation email for "Pay later" option
                    logger.LogInformation("Sending setup confirmation email for setup intent {SetupIntentId}", setupIntent.Id);
                    
                    quoteDto.Payment.Status = PaymentStatus.PaymentSetup;
                    await UpdateQuoteAsync(new SaveQuoteRequest
                    {
                        Quote = quoteDto,
                        Customer = userMapper.ToDto(user!)
                    });
                    
                    var templateData = new
                    {
                        customerName = user?.FullName,
                        totalAmount = totalCost.ToString("N2"),
                        quoteReference,
                        setupDate = setupDate.ToString("dddd, MMMM dd, yyyy"),
                        setupTime = setupDate.ToString("HH:mm") + " GMT",
                        currentYear = DateTime.UtcNow.Year
                    };
                    
                    var subject = $"Payment Setup Confirmation - #{quoteReference}";
                    var htmlEmail = templateService.GenerateEmail("setup-confirmation.html", templateData);
                    var textEmail = templateService.GenerateEmail("setup-confirmation.txt", templateData);
                    
                    await awsEmailService.SendBookingConfirmationEmailAsync(subject, customer.Email, htmlEmail, textEmail);
                    
                    logger.LogInformation("Setup confirmation email sent for setup intent {SetupIntentId}", setupIntent.Id);
                }
            }
            else
            {
                logger.LogWarning("Could not send setup confirmation email - customer email not found for setup intent {SetupIntentId}", setupIntent.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send setup confirmation email for setup intent {SetupIntentId}", setupIntent.Id);
            // Don't throw here to avoid failing the webhook
        }
    }

    [HttpGet("test-template/{templateName}")]
    public IActionResult TestTemplate(string templateName)
    {
        try
        {
            var testData = new
            {
                customerName = "John Doe",
                depositAmount = "125.00",
                totalAmount = "500.00",
                remainingAmount = "375.00",
                balanceAmount = "375.00",
                quoteReference = "TEST123",
                paymentDate = "Monday, January 15, 2024",
                paymentTime = "14:30 GMT",
                setupDate = "Monday, January 15, 2024",
                setupTime = "14:30 GMT",
                currentYear = DateTime.UtcNow.Year
            };

            var result = templateService.GenerateEmail(templateName, testData);
            return Ok(new { templateName, result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}