using Microsoft.AspNetCore.Mvc;
using Stripe;
using TranzrMoves.Api.Dtos;
using TranzrMoves.Api.Services;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class CheckoutController(StripeClient stripeClient, 
    IConfiguration configuration, 
    ILogger<CheckoutController> logger, 
    IAwsEmailService awsEmailService,
    IEmailService emailService) : ApiControllerBase
{
    [HttpPost("payment-sheet", Name = "CreateStripeIntent")]
    public async Task<ActionResult<PaymentSheetCreateResponse>> CreatePaymentSheet([FromBody] PaymentSheetRequest paymentSheetRequest)
    {
        // Use an existing Customer ID if this is a returning customer.
        logger.LogInformation("Creating payment sheet");
        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{paymentSheetRequest.Customer.Email}'",
        });
        
        var customer = customerSearchResult.Data.FirstOrDefault();
        
        if (customer is null)
        { 
            logger.LogInformation("Customer does not exist. Creating customer with {email}",  paymentSheetRequest.Customer.Email);

            var splitAddress = paymentSheetRequest.Customer.BillingAddress.Line1.Split(',');
            var city = splitAddress[^2].Trim();
            var customerOptions = new CustomerCreateOptions
            {
                Email = paymentSheetRequest.Customer.Email,
                Name = paymentSheetRequest.Customer.FullName,
                Address = new AddressOptions
                {
                    Line1 = splitAddress.FirstOrDefault()?.Trim(),
                    Line2 = splitAddress.Length == 4 ? splitAddress[2].Trim() : string.Empty,
                    City = city,
                    PostalCode = splitAddress.LastOrDefault()?.Trim(),
                    Country = "United Kingdom"
                }
            };
            
            customer = await stripeClient.V1.Customers.CreateAsync(customerOptions);
        }
        
        var ephemeralKeyOptions = new EphemeralKeyCreateOptions
        {
            Customer = customer.Id,
            StripeVersion = "2025-06-30.basil",
        };
        
        var ephemeralKey = await stripeClient.V1.EphemeralKeys.CreateAsync(ephemeralKeyOptions);

        // Determine payment amount based on payment type
        long paymentAmount = 0;
        string description = "Your Tranzr Moves payment";
        
        switch (paymentSheetRequest.PaymentType)
        {
            case PaymentType.Full:
                paymentAmount = (long)Math.Round(paymentSheetRequest.Cost.Total * 100, 0, MidpointRounding.AwayFromZero);
                description = "Your Tranzr Moves payment - Full amount";
                break;
                
            case PaymentType.Deposit:
                // Calculate deposit amount from percentage for security
                var depositPercentage = paymentSheetRequest.DepositPercentage ?? 25m; // Default to 25% if not specified
                var calculatedDepositAmount = (decimal)paymentSheetRequest.Cost.Total * (depositPercentage / 100m);
                paymentAmount = (long)Math.Round(calculatedDepositAmount * 100, 0, MidpointRounding.AwayFromZero);
                
                // Log the calculation for audit
                logger.LogInformation("Deposit calculation: Total {Total} * {Percentage}% = {DepositAmount}", 
                    paymentSheetRequest.Cost.Total, depositPercentage, calculatedDepositAmount);
                
                description = $"Your Tranzr Moves payment - {depositPercentage}% deposit";
                break;
                
            case PaymentType.Later:
                // For "pay later", we create a Setup Intent for saving payment methods
                paymentAmount = 0;
                description = "Your Tranzr Moves payment - Payment deferred";
                break;
        }

        // Handle different payment types
        if (paymentSheetRequest.PaymentType == PaymentType.Later)
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
                    { "payment_type", paymentSheetRequest.PaymentType.ToString() },
                    { "total_cost", paymentSheetRequest.Cost.Total.ToString("F2") },
                    { "deposit_percentage", paymentSheetRequest.DepositPercentage?.ToString() ?? "0" },
                    { "due_date", paymentSheetRequest.DueDate?.ToString("yyyy-MM-dd") ?? "" },
                    { "booking_id", paymentSheetRequest.BookingId ?? "" }
                }
            };
            
            var setupIntent = await stripeClient.V1.SetupIntents.CreateAsync(setupIntentOptions);
            
            logger.LogInformation("Setup intent created for {PaymentType} to save payment method", 
                paymentSheetRequest.PaymentType);
            
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
            ReceiptEmail = paymentSheetRequest.Customer.Email,
            
            // Add metadata for payment tracking
            Metadata = new Dictionary<string, string>
            {
                { "payment_type", paymentSheetRequest.PaymentType.ToString() },
                { "total_cost", paymentSheetRequest.Cost.Total.ToString("F2") },
                { "deposit_percentage", paymentSheetRequest.DepositPercentage?.ToString() ?? "0" },
                { "due_date", paymentSheetRequest.DueDate?.ToString("yyyy-MM-dd") ?? "" },
                { "booking_id", paymentSheetRequest.BookingId ?? "" }
            }
        };
            
        // Set up future usage for deposits
        if (paymentSheetRequest.PaymentType == PaymentType.Full)
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
            logger.LogInformation("PaymentIntent created with setup_future_usage for {PaymentType}", 
                paymentSheetRequest.PaymentType);
        }
            
        var paymentIntent = await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions);
            
        logger.LogInformation("Payment intent created for {PaymentType} with amount {Amount}", 
            paymentSheetRequest.PaymentType, paymentAmount);
            
        return new PaymentSheetCreateResponse
        {
            PaymentIntent = paymentIntent.ClientSecret,
            PaymentIntentId = paymentIntent.Id,
            EphemeralKey = ephemeralKey.Secret,
            Customer = customer.Id,
            PublishableKey = StripeConfiguration.ClientId
        };
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
                    { "payment_type", "balance" },
                    { "original_booking_id", request.BookingId },
                    { "deposit_paid", "true" },
                    { "customer_name", request.CustomerName },
                    { "payment_method_id", paymentMethod.Id },
                    { "original_total_cost", request.OriginalTotalCost.ToString("F2") },
                    { "original_deposit_amount", request.OriginalDepositAmount.ToString("F2") },
                    { "calculated_remaining_amount", expectedRemainingAmount.ToString("F2") },
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
                
                // Handle successful payment method setup for "Pay later" option
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
            
            if (customer != null && !string.IsNullOrEmpty(customer.Email))
            {
                var orderId = paymentIntent.Id;
                var orderDate = DateTime.UtcNow;
                var customerName = customer.Name ?? customer.Email.Split('@')[0];
                
                // Check payment type from metadata
                var paymentType = paymentIntent.Metadata.GetValueOrDefault("payment_type", "full");
                var totalCost = decimal.Parse(paymentIntent.Metadata.GetValueOrDefault("total_cost", "0"));
                var depositPercentage = paymentIntent.Metadata.GetValueOrDefault("deposit_percentage", "0");
                
                if (paymentType == "deposit")
                {
                    // Send deposit confirmation email
                    logger.LogInformation("Sending deposit confirmation email for payment intent {PaymentIntentId}", paymentIntent.Id);
                    
                    // Note: You'll need to implement SendDepositConfirmationEmailAsync in your email service
                    // For now, we'll use the regular confirmation email with deposit context
                    await awsEmailService.SendOrderConfirmationEmailAsync(
                        customer.Email,
                        customerName,
                        paymentIntent.Amount, // Already in pence
                        orderId,
                        orderDate
                    );
                    
                    logger.LogInformation("Deposit confirmation email sent for payment intent {PaymentIntentId}", paymentIntent.Id);
                }
                else if (paymentType == "balance")
                {
                    // Send balance payment confirmation email
                    logger.LogInformation("Sending balance payment confirmation email for payment intent {PaymentIntentId}", paymentIntent.Id);
                    
                    await awsEmailService.SendOrderConfirmationEmailAsync(
                        customer.Email,
                        customerName,
                        paymentIntent.Amount, // Already in pence
                        orderId,
                        orderDate
                    );
                    
                    logger.LogInformation("Balance payment confirmation email sent for payment intent {PaymentIntentId}", paymentIntent.Id);
                }
                else
                {
                    // Send regular order confirmation email for full payments
                    await awsEmailService.SendOrderConfirmationEmailAsync(
                        customer.Email,
                        customerName,
                        paymentIntent.Amount, // Already in pence
                        orderId,
                        orderDate
                    );
                    
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
            
            if (customer != null && !string.IsNullOrEmpty(customer.Email))
            {
                var setupId = setupIntent.Id;
                var setupDate = DateTime.UtcNow;
                var customerName = customer.Name ?? customer.Email.Split('@')[0];
                
                // Check payment type from metadata
                var paymentType = setupIntent.Metadata.GetValueOrDefault("payment_type", "later");
                var totalCost = decimal.Parse(setupIntent.Metadata.GetValueOrDefault("total_cost", "0"));
                var dueDate = setupIntent.Metadata.GetValueOrDefault("due_date", "");
                
                if (paymentType == "later")
                {
                    // Send setup confirmation email for "Pay later" option
                    logger.LogInformation("Sending setup confirmation email for setup intent {SetupIntentId}", setupIntent.Id);
                    
                    // Note: You'll need to implement SendSetupIntentConfirmationEmailAsync in your email service
                    // For now, we'll use the regular confirmation email with setup context
                    await awsEmailService.SendOrderConfirmationEmailAsync(
                        customer.Email,
                        customerName,
                        0, // No immediate charge for setup
                        setupId,
                        setupDate
                    );
                    
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
}