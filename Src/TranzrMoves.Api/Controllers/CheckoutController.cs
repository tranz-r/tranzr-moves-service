using System.Globalization;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.Text;

using Stripe;
using Stripe.Checkout;
using Stripe.Tax;

using TranzrMoves.Api.Dtos;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Quote.Save;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

using Quote = TranzrMoves.Domain.Entities.Quote;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class CheckoutController(
    StripeClient stripeClient,
    IConfiguration configuration,
    ILogger<CheckoutController> logger,
    IQuoteRepository quoteRepository,
    IUserRepository userRepository,
    IUserQuoteRepository userQuoteRepository,
    IMediator mediator,
    IEmailService emailService,
    ITemplateService templateService,
    ITimeService timeService) : ApiControllerBase
{
    private const string TaxCode = "txcd_20030000";
    private const string TaxBehavior = "inclusive";
    private const string Currency = "gbp";

    private static readonly LocalDatePattern UtcEmailDatePattern =
        LocalDatePattern.CreateWithInvariantCulture("dddd, MMMM dd, yyyy");

    private static readonly LocalTimePattern UtcEmailTimePattern =
        LocalTimePattern.CreateWithInvariantCulture("HH:mm");

    private static string FormatInstantAsEmailDate(Instant instant) =>
        UtcEmailDatePattern.Format(instant.InUtc().Date);

    private static string FormatInstantAsEmailTimeUtc(Instant instant) =>
        UtcEmailTimePattern.Format(instant.InUtc().TimeOfDay) + " GMT";

    private static string FormatLocalDateIso(LocalDate? d) =>
        d is { } v ? LocalDatePattern.Iso.Format(v) : string.Empty;

    [HttpPost("session")]
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.QuoteReference) || request.Amount <= 0)
            {
                return BadRequest("quoteReference and positive amount are required");
            }

            var quote = await quoteRepository.GetQuoteByReferenceAsync(request.QuoteReference, cancellationToken);
            if (quote is null)
            {
                return NotFound("Quote not found");
            }

            var customerQuote = await userQuoteRepository.GetUserQuoteByQuoteIdAsync(quote.Id, cancellationToken);
            if (customerQuote is null)
            {
                return BadRequest("Customer quote relationship not found");
            }

            var user = await userRepository.GetUserAsync(customerQuote.UserId, cancellationToken);
            if (user is null || string.IsNullOrEmpty(user.Email))
            {
                return BadRequest("Customer email is required");
            }

            return await CreateCheckoutSession(request, user, quote,
                "checkout-session",
                new KeyValuePair<string, string>(nameof(PaymentMetadata.PaymentType), nameof(PaymentType.Adhoc)),
                cancellationToken);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating checkout session for quote {QuoteReference}",
                request!.QuoteReference);
            return BadRequest($"Checkout session creation failed: {ex.StripeError?.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create checkout session for quote {QuoteReference}", request.QuoteReference);
            return StatusCode(500, "Failed to create checkout session");
        }
    }

    private async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession(
        CreateCheckoutSessionRequest request, User user,
        Quote quote, string emailTemplateName, KeyValuePair<string, string> metadata,
        CancellationToken cancellationToken, string? cardErrorReason = null, List<string>? bbcRecipients = null)
    {
        // Create or find stripe customer
        var sr = await stripeClient.V1.Customers.SearchAsync(
            new CustomerSearchOptions { Query = $"email:'{user.Email}'" }, cancellationToken: cancellationToken);

        var stripeCustomer = sr.Data.FirstOrDefault();

        if (stripeCustomer is null)
        {
            stripeCustomer = await stripeClient.V1.Customers.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.FullName,
                Address = user.BillingAddress != null
                    ? new AddressOptions
                    {
                        Line1 = user.BillingAddress.Line1,
                        Line2 = user.BillingAddress.Line2,
                        City = user.BillingAddress.City,
                        PostalCode = user.BillingAddress.PostCode,
                        Country = user.BillingAddress.Country ?? "United Kingdom",
                        // Enhanced with extended Mapbox fields
                        State = user.BillingAddress.Region
                    }
                    : null
            }, cancellationToken: cancellationToken);
        }

        // Create Price for this amount/description
        var priceId = await CreateOrGetPriceAsync(request.Amount,
            string.IsNullOrWhiteSpace(request.Description)
                ? $"Payment for quote #{quote.QuoteReference}"
                : request.Description, quote.QuoteReference);

        // Create Checkout Session
        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            Customer = stripeCustomer.Id,
            ClientReferenceId = quote.QuoteReference,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            ],
            AutomaticTax = new SessionAutomaticTaxOptions
            {
                Enabled = true // Enable automatic tax calculation
            },
            Metadata = new Dictionary<string, string>
            {
                { nameof(PaymentMetadata.QuoteReference), quote.QuoteReference },
                { nameof(PaymentMetadata.CustomerEmail), user.Email },
                { nameof(PaymentMetadata.PaymentAmount), request.Amount.ToString("F2") },
                { nameof(PaymentMetadata.ExtraChargesDescription), request.Description },
                { metadata.Key, metadata.Value }
            },
            SuccessUrl = configuration["CHECKOUT_SESSION_SUCCESS_URL"],
            CancelUrl = configuration["CHECKOUT_SESSION_CANCEL_URL"]
        };

        var sessionService = new SessionService(stripeClient);
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        quote.StripeSessionId = session.Id;
        await quoteRepository.UpdateQuoteAsync(quote, cancellationToken);

        // Email checkout URL to customer
        bool emailSent = false;
        try
        {
            var templateData = new
            {
                customerName = user.FullName,
                paymentAmount = request.Amount.ToString("N2"),
                quoteReference = quote.QuoteReference,
                checkoutUrl = session.Url,
                description = string.IsNullOrWhiteSpace(request.Description)
                    ? $"Payment for quote #{quote.QuoteReference}"
                    : request.Description,
                currentYear = timeService.NowInUtc().Year,
                cardErrorReason
            };

            var subject = $"Checkout Link - #{quote.QuoteReference}";
            var htmlEmail = templateService.GenerateEmail($"{emailTemplateName}.html", templateData);
            var textEmail = templateService.GenerateEmail($"{emailTemplateName}.txt", templateData);
            await emailService.SendBookingConfirmationEmailAsync(FromEmails.Booking, subject, user.Email, htmlEmail,
                textEmail, bbcRecipients);
            emailSent = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send checkout session email to {Email}", user.Email);
        }

        return Ok(new CreateCheckoutSessionResponse
        {
            SessionId = session.Id,
            Url = session.Url,
            QuoteReference = quote.QuoteReference,
            Amount = request.Amount,
            EmailSent = emailSent
        });
    }

    [HttpGet("session")]
    public async Task<ActionResult<GetCheckoutSessionResponse>> GetCheckoutSession([FromQuery] string id,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("id is required");

        var sessionService = new SessionService(stripeClient);
        var session = await sessionService.GetAsync(id, cancellationToken: ct);
        return Ok(new GetCheckoutSessionResponse
        {
            SessionId = session.Id,
            CustomerId = session.CustomerId,
            PaymentIntentId = session.PaymentIntentId,
            Status = session.Status,
            PaymentStatus = session.PaymentStatus,
            Url = session.Url
        });
    }

    [HttpPost("payment-sheet", Name = "CreateStripeIntent")]
    public async Task<ActionResult<PaymentSheetCreateResponse>> CreatePaymentSheet(
        [FromBody] SaveQuoteRequest? saveQuoteRequest, CancellationToken ct)
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
            logger.LogInformation("Customer does not exist. Creating customer with {email}",
                saveQuoteRequest?.Customer?.Email);

            var customerOptions = new CustomerCreateOptions
            {
                Email = saveQuoteRequest?.Customer?.Email,
                Name = saveQuoteRequest?.Customer?.FullName,
                Address = saveQuoteRequest?.Customer?.BillingAddress != null
                    ? new AddressOptions
                    {
                        Line1 = saveQuoteRequest.Customer.BillingAddress.Line1,
                        Line2 = saveQuoteRequest.Customer.BillingAddress.Line2,
                        City = saveQuoteRequest.Customer.BillingAddress.City,
                        PostalCode = saveQuoteRequest.Customer.BillingAddress.PostCode,
                        Country = saveQuoteRequest.Customer.BillingAddress.Country ?? "United Kingdom",
                        // Enhanced with extended Mapbox fields
                        State = saveQuoteRequest.Customer.BillingAddress.Region
                    }
                    : null
            };

            customer = await stripeClient.V1.Customers.CreateAsync(customerOptions, cancellationToken: ct);
        }
        else
        {
            var customerOptions = new CustomerUpdateOptions
            {
                Name = saveQuoteRequest?.Customer?.FullName,
                Address = saveQuoteRequest?.Customer?.BillingAddress != null
                    ? new AddressOptions
                    {
                        Line1 = saveQuoteRequest.Customer.BillingAddress.Line1,
                        Line2 = saveQuoteRequest.Customer.BillingAddress.Line2,
                        City = saveQuoteRequest.Customer.BillingAddress.City,
                        PostalCode = saveQuoteRequest.Customer.BillingAddress.PostCode,
                        Country = saveQuoteRequest.Customer.BillingAddress.Country,
                        // Enhanced with extended Mapbox fields
                        // State = saveQuoteRequest.Customer.BillingAddress.Region
                    }
                    : null
            };

            customer = await stripeClient.V1.Customers.UpdateAsync(customer.Id, customerOptions, cancellationToken: ct);
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
                paymentAmount = (long)Math.Round((decimal)(saveQuoteRequest.Quote.Pricing?.TotalCost * 100)!, 0,
                    MidpointRounding.AwayFromZero);
                description = "Your Tranzr Moves payment - Full amount";
                break;

            case PaymentType.Deposit:
                // Calculate deposit amount from percentage for security
                var depositPercentage = 25m; // Default to 25% if not specified
                var calculatedDepositAmount =
                    (decimal)saveQuoteRequest.Quote.Pricing?.TotalCost! * (depositPercentage / 100m);
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
            var moveDate = saveQuoteRequest!.Quote.Schedule!.DateISO!.Value;
            var paymentDueDate = moveDate.PlusDays(-3); // calendar analogue to 72h before date-only move

            // Create Setup Intent for "pay later" option
            var setupIntentOptions = new SetupIntentCreateOptions
            {
                Customer = customer.Id,
                Description = description,
                Usage = "off_session", // Enable automatic charging later

                PaymentMethodTypes = ["card", "link"],

                // Add metadata for payment tracking
                Metadata = new Dictionary<string, string>
                {
                    { nameof(PaymentMetadata.PaymentType), saveQuoteRequest.Quote.Payment.PaymentType.ToString() },
                    { nameof(PaymentMetadata.TotalCost), saveQuoteRequest.Quote.Pricing?.TotalCost?.ToString("F2")! },
                    {
                        nameof(PaymentMetadata.DepositPercentage),
                        saveQuoteRequest.Quote.Payment.DepositPercentage?.ToString() ?? "0"
                    },
                    { nameof(PaymentMetadata.DueDate), FormatLocalDateIso(paymentDueDate) },
                    { nameof(PaymentMetadata.QuoteReference), saveQuoteRequest.Quote.QuoteReference ?? "" },
                    { nameof(PaymentMetadata.QuoteId), saveQuoteRequest.Quote.Id.ToString() },
                    {
                        nameof(PaymentMetadata.PaymentDueDate),
                        FormatLocalDateIso(saveQuoteRequest.Quote.Payment.DueDate)
                    }
                }
            };

            var setupIntent = await stripeClient.V1.SetupIntents.CreateAsync(setupIntentOptions, cancellationToken: ct);

            logger.LogInformation("Setup intent created for {PaymentType} to save payment method",
                saveQuoteRequest.Quote.Payment.PaymentType);

            saveQuoteRequest.Quote.Payment.PaymentIntentId = setupIntent.Id;
            saveQuoteRequest.Quote.Payment.DueDate = paymentDueDate; // ~72h before move (calendar days)

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

        // 1. Create a tax calculation with tax-inclusive pricing
        var calculationOptions = new CalculationCreateOptions
        {
            Currency = Currency,
            LineItems =
            [
                new CalculationLineItemOptions
                {
                    Amount = paymentAmount,
                    Reference = saveQuoteRequest!.Quote.QuoteReference,
                    TaxCode = TaxCode,
                    TaxBehavior = TaxBehavior // This specifies that the price already includes tax
                }
            ],
            Customer = customer.Id
        };

        var taxCalculationService = stripeClient.V1.Tax.Calculations;
        Calculation calculation = await taxCalculationService.CreateAsync(calculationOptions, cancellationToken: ct);

        // 2. Create PaymentIntent for Full and Deposit payments
        var paymentIntentOptions = new PaymentIntentCreateOptions
        {
            Amount = calculation.AmountTotal,
            Currency = Currency,
            Customer = customer.Id,
            Description = description,
            ReceiptEmail = saveQuoteRequest?.Customer?.Email,

            // Add metadata for payment tracking
            Metadata = new Dictionary<string, string>
            {
                { nameof(PaymentMetadata.PaymentType), saveQuoteRequest!.Quote.Payment?.PaymentType.ToString()! },
                { nameof(PaymentMetadata.TotalCost), saveQuoteRequest.Quote.Pricing?.TotalCost?.ToString("F2")! },
                {
                    nameof(PaymentMetadata.DepositPercentage),
                    saveQuoteRequest.Quote.Payment!.DepositPercentage?.ToString() ?? "0"
                },
                {
                    nameof(PaymentMetadata.DueDate),
                    FormatLocalDateIso(saveQuoteRequest.Quote.Payment.DueDate)
                },
                { nameof(PaymentMetadata.QuoteReference), saveQuoteRequest.Quote.QuoteReference ?? "" },
                { nameof(PaymentMetadata.QuoteId), saveQuoteRequest.Quote.Id.ToString() }
            }
        };

        // Set up future usage for deposits
        if (saveQuoteRequest is { Quote.Payment.PaymentType: PaymentType.Full })
        {
            logger.LogInformation("PaymentIntent created with automatic payment methods for {PaymentType}",
                saveQuoteRequest.Quote.Payment.PaymentType);

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

        var paymentIntent =
            await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions, cancellationToken: ct);

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

    private async Task<string> CreateOrGetPriceAsync(decimal amount, string description, string quoteReference)
    {
        try
        {
            // Create a unique product name for this quote
            var productName = $"Tranzr Moves - Quote #{quoteReference}";

            // First, try to find an existing product
            var productSearchOptions = new ProductSearchOptions
            {
                Query = $"name:'{productName}'",
                Limit = 1
            };

            var existingProducts = await stripeClient.V1.Products.SearchAsync(productSearchOptions);

            Product product;
            if (existingProducts.Data.Any())
            {
                product = existingProducts.Data.First();
            }
            else
            {
                // Create new product
                var productOptions = new ProductCreateOptions
                {
                    Name = productName,
                    Description = description,
                    Type = "service"
                };
                product = await stripeClient.V1.Products.CreateAsync(productOptions);
            }

            // Create price for this product
            var priceOptions = new PriceCreateOptions
            {
                Product = product.Id,
                Currency = Currency,
                UnitAmount = (long)Math.Round(amount * 100, 0, MidpointRounding.AwayFromZero),
                TaxBehavior = TaxBehavior // Tell Stripe that tax is included in the price
            };

            var price = await stripeClient.V1.Prices.CreateAsync(priceOptions);
            return price.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating or getting Stripe price for amount {Amount}", amount);
            throw;
        }
    }

    private async Task<SaveQuoteResponse> UpdateQuoteAsync(SaveQuoteRequest saveQuoteRequest,
        CancellationToken ct = default)
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

    [HttpPost("deposit-balance-payment", Name = "CreateFuturePayment")]
    public async Task<ActionResult<PaymentIntentResponse>> CreateFuturePayment([FromBody] FuturePaymentRequest request,
        CancellationToken cancellationToken)
    {
        // This endpoint handles creating PaymentIntents for the remaining balance
        // after a deposit has been paid, using saved payment methods

        User? user = null;
        try
        {
            var quote = await quoteRepository.GetQuoteByReferenceAsync(request.QuoteReference, cancellationToken);
            if (quote == null)
            {
                logger.LogWarning("Quote not found for future payment: {QuoteReference}", request.QuoteReference);
                return NotFound("Quote not found");
            }

            var customerQuote = await userQuoteRepository.GetUserQuoteByQuoteIdAsync(quote.Id, cancellationToken);
            if (customerQuote == null)
            {
                logger.LogWarning("Customer quote relationship not found for quote {QuoteReference}",
                    request.QuoteReference);
                return BadRequest("Customer quote relationship not found");
            }

            user = await userRepository.GetUserAsync(customerQuote.UserId, cancellationToken);

            logger.LogInformation("Creating future payment for customer {CustomerId} with remaining amount {Amount}",
                user!.Id, quote.TotalCost - quote.DepositAmount);

            var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
            {
                Query = $"email:'{user.Email}'",
            }, cancellationToken: cancellationToken);

            var customer = customerSearchResult.Data.FirstOrDefault();

            if (customer == null)
            {
                logger.LogWarning("Customer not found for future payment for quote: {QuoteReference}",
                    request.QuoteReference);
                return BadRequest("Customer not found");
            }

            var extraCharges = request.ExtraCharges ?? 0m;
            var remainingAmount = quote.TotalCost!.Value - quote.DepositAmount!.Value;
            var chargeableAmount = remainingAmount + extraCharges * 1.2m;
            var amountInCurrencyUnit = (long)Math.Round(chargeableAmount * 100, 0, MidpointRounding.AwayFromZero);

            // 1. Create a tax calculation with tax-inclusive pricing
            var calculationOptions = new CalculationCreateOptions
            {
                Currency = Currency,
                LineItems =
                [
                    new CalculationLineItemOptions
                    {
                        Amount = amountInCurrencyUnit,
                        Reference = request.QuoteReference,
                        TaxCode = TaxCode,
                        TaxBehavior = TaxBehavior // This specifies that the price already includes tax
                    }
                ],
                Customer = customer.Id
            };

            var taxCalculationService = stripeClient.V1.Tax.Calculations;
            Calculation calculation =
                await taxCalculationService.CreateAsync(calculationOptions, cancellationToken: cancellationToken);

            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = calculation.AmountTotal,
                Currency = Currency,
                Customer = customer.Id,
                PaymentMethod = quote.PaymentMethodId, // Use the saved payment method
                Description = "Tranzr Moves - Remaining balance payment",
                ReceiptEmail = customer.Email,
                Confirm = true, // Automatically confirm the payment
                OffSession = true, // This is an off-session payment (automatic)
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "always"
                },
                Metadata = new Dictionary<string, string>
                {
                    { nameof(PaymentMetadata.PaymentType), nameof(PaymentType.Balance) },
                    { nameof(PaymentMetadata.QuoteReference), request.QuoteReference }
                }
            };

            if (request.ExtraCharges != null)
            {
                paymentIntentOptions.Metadata.Add(nameof(PaymentMetadata.ExtraCharges), extraCharges.ToString("N"));
                paymentIntentOptions.Metadata.Add(nameof(PaymentMetadata.ExtraChargesDescription),
                    request.ExtraChargesDescription);
            }

            var paymentIntent =
                await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions,
                    cancellationToken: cancellationToken);

            logger.LogInformation(
                "Future payment intent created {PaymentIntentId} for remaining amount {Amount} using saved payment method",
                paymentIntent.Id, chargeableAmount);

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
                logger.LogWarning(
                    "Payment requires authentication for customer with Id {UserId}. Customer needs to complete 3D Secure for quote {QuoteReference}",
                    user!.Id, request.QuoteReference);

                return BadRequest(
                    "Payment requires additional authentication. Please contact customer to complete payment.");
            }

            if (user != null)
                logger.LogError(ex,
                    "Stripe error creating future payment for customer {UserId}: {ErrorCode} - {ErrorMessage}",
                    user.Id, ex.StripeError.Code, ex.StripeError.Message);
            return BadRequest($"Payment failed: {ex.StripeError.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create future payment for customer {UserId}", user!.Id);
            return StatusCode(500, "Failed to create future payment");
        }
    }


    [HttpPost("pay-later-collection")]
    public async Task<ActionResult<PaymentIntentResponse>> CollectLaterPayment(CancellationToken cancellationToken)
    {
        var payLaterQuotes = await quoteRepository
            .GetPayLaterQuotesForTodayAsync(timeService.TodayInUtc(), cancellationToken);

        foreach (var quote in payLaterQuotes)
        {
            _ = await ProcessLaterPayment(new FuturePaymentRequest
            {
                QuoteReference = quote.QuoteReference
            }, cancellationToken);
        }

        return NoContent();
    }

    private async Task<ErrorOr<Success>> ProcessLaterPayment(FuturePaymentRequest request,
        CancellationToken cancellationToken)
    {
        // This endpoint handles creating PaymentIntents for the remaining balance
        // after a deposit has been paid, using saved payment methods

        User? user = null;

        var quote = await quoteRepository.GetQuoteByReferenceAsync(request.QuoteReference, cancellationToken);
        if (quote == null)
        {
            logger.LogWarning("Quote not found for future payment: {QuoteReference}", request.QuoteReference);
            return Error.NotFound("Quote not found");
        }

        var customerQuote = await userQuoteRepository.GetUserQuoteByQuoteIdAsync(quote.Id, cancellationToken);
        if (customerQuote == null)
        {
            logger.LogWarning("Customer quote relationship not found for quote {QuoteReference}",
                request.QuoteReference);
            return Error.Failure("Customer quote relationship not found");
        }

        user = await userRepository.GetUserAsync(customerQuote.UserId, cancellationToken);

        logger.LogInformation(
            "Collecting payment for pay later for customer {CustomerId} for the full amount of £{Amount}",
            user!.Id, quote.TotalCost - quote.DepositAmount);

        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{user.Email}'",
        }, cancellationToken: cancellationToken);

        var customer = customerSearchResult.Data.FirstOrDefault();

        if (customer == null)
        {
            logger.LogWarning("Customer not found for future payment for quote: {QuoteReference}",
                request.QuoteReference);
            return Error.Failure("Customer not found");
        }

        var amountInCurrencyUnit = (long)Math.Round(quote.TotalCost!.Value * 100, 0, MidpointRounding.AwayFromZero);

        // 1. Create a tax calculation with tax-inclusive pricing
        var calculationOptions = new CalculationCreateOptions
        {
            Currency = Currency,
            LineItems =
            [
                new CalculationLineItemOptions
                {
                    Amount = amountInCurrencyUnit,
                    Reference = request.QuoteReference,
                    TaxCode = TaxCode,
                    TaxBehavior = TaxBehavior // This specifies that the price already includes tax
                }
            ],
            Customer = customer.Id
        };

        var taxCalculationService = stripeClient.V1.Tax.Calculations;
        Calculation calculation =
            await taxCalculationService.CreateAsync(calculationOptions, cancellationToken: cancellationToken);

        var paymentIntentOptions = new PaymentIntentCreateOptions
        {
            Amount = calculation.AmountTotal,
            Currency = Currency,
            Customer = customer.Id,
            PaymentMethod = quote.PaymentMethodId, // Use the saved payment method
            Description = "Tranzr Moves - Remaining balance payment",
            ReceiptEmail = customer.Email,
            Confirm = true, // Automatically confirm the payment
            OffSession = true, // This is an off-session payment (automatic)
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "always"
            },
            Metadata = new Dictionary<string, string>
            {
                { nameof(PaymentMetadata.PaymentType), nameof(PaymentType.Balance) },
                { nameof(PaymentMetadata.QuoteReference), request.QuoteReference }
            }
        };

        try
        {
            var paymentIntent =
                await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions,
                    cancellationToken: cancellationToken);

            logger.LogInformation(
                "Future payment intent created {PaymentIntentId} for remaining amount {Amount} using saved payment method",
                paymentIntent.Id, quote.TotalCost!.Value);

            return new ErrorOr<Success>();
        }
        catch (StripeException ex)
        {
            if (ex.StripeError.Type == "card_error")
            {
                var cardErrorReason = string.Empty;
                var paymentMethod = await stripeClient.V1.PaymentMethods.GetAsync(quote.PaymentMethodId, cancellationToken: cancellationToken);
                var cardLast4Digits = paymentMethod.Card.Last4;
                var cardBrand = paymentMethod.Card.Brand;

                switch (ex.StripeError.Code)
                {
                    case "card_declined" when ex.StripeError.DeclineCode == "insufficient_funds":
                        cardErrorReason  = "the card was declined due to insufficient funds";
                        logger.LogWarning(ex, "the was declined.");
                        break;
                    case "card_declined":
                        cardErrorReason  = "the was declined.";
                        logger.LogWarning(ex, "the was declined.");
                        break;
                    case "expired_card":
                        cardErrorReason  = "the card has expired.";
                        logger.LogWarning(ex, "the has expired.");
                        break;
                    case "incorrect_cvc":
                        cardErrorReason = "the card's security code is incorrect.";
                        logger.LogWarning(ex, "the card's security code is incorrect.");
                        break;
                    case "incorrect_number":
                        cardErrorReason = "the card number is incorrect.";
                        logger.LogWarning(ex, "the card's security code is incorrect.");
                        break;
                    case "invalid_cvc":
                        cardErrorReason = "the card's security code is invalid.";
                        logger.LogWarning(ex, "the card's security code is invalid.");
                        break;
                    case "invalid_expiry_month":
                        cardErrorReason = "the expiry month is invalid.";
                        logger.LogWarning(ex, "the expiry month is invalid.");
                        break;
                    case "invalid_expiry_year":
                        cardErrorReason = "the expiry year is invalid.";
                        logger.LogWarning(ex, "the expiry year is invalid.");
                        break;
                    case "processing_error":
                        cardErrorReason = "an error occurred while processing the card.";
                        logger.LogWarning(ex, "an error occurred while processing the card.");
                        break;
                    case "incorrect_zip":
                        cardErrorReason = "the card's post code failed validation.";
                        logger.LogWarning(ex, "the card's post code failed validation.");
                        break;
                    case "authentication_required":
                        cardErrorReason = "the card's authorization is required";
                        logger.LogWarning(ex, "the card's authorization is required.");
                        break;
                    case "approve_with_id":
                        cardErrorReason = "the payment cannot be authorized";
                        logger.LogWarning(ex, "the payment cannot be authorized.");
                        break;
                    case "call_issuer":
                        cardErrorReason = "the card has been declined, call issuer";
                        logger.LogWarning(ex, "the card has been declined.");
                        break;
                    case "do_not_honor":
                        cardErrorReason = "the card has been declined";
                        logger.LogWarning(ex, "the card has been declined.");
                        break;
                    case "insufficient_funds":
                        cardErrorReason = "the card has insufficient funds.";
                        logger.LogWarning(ex, "the card has insufficient funds.");
                        break;
                    case "invalid_account":
                        cardErrorReason = "the card or account is invalid.";
                        logger.LogWarning(ex, "the card or account is invalid.");
                        break;
                    case "currency_not_supported":
                        cardErrorReason = "the card does not support this currency.";
                        logger.LogWarning(ex, "the card does not support this currency.");
                        break;
                    case "lost_card":
                        cardErrorReason = "the card has been reported lost.";
                        logger.LogWarning(ex, "the card has been reported lost.");
                        break;
                    case "stolen_card":
                        cardErrorReason = "the card has been reported stolen.";
                        logger.LogWarning(ex, "the card has been reported stolen.");
                        break;
                    default:
                        Console.WriteLine($"Other card error: {ex.StripeError.Message}");
                        break;
                }

                var cardErrorDescription = $"<b>We couldn't charge your {cardBrand} card ending with {cardLast4Digits}, because {cardErrorReason}.</b> " +
                                           $"<p>Please use the secure link below to complete the payment for your quotation {quote.QuoteReference} with Tranzr Moves.</p> " +
                                           $"To avoid any delay or cancellation of your scheduled service on " +
                                           $"{FormatLocalDateIso(quote.CollectionDate)}, kindly make your payment as soon as possible or contact us to arrange an alternative payment option.";

                await CreateCheckoutSession(new CreateCheckoutSessionRequest
                    {
                        Amount = quote.TotalCost!.Value,
                        QuoteReference = quote.QuoteReference,
                        Description = $"Full payment for quote #{quote.QuoteReference}"
                    }, user, quote, "checkout-session-payment-error",
                    new KeyValuePair<string, string>(nameof(PaymentMetadata.PaymentType), nameof(PaymentType.Full)),
                    cancellationToken, cardErrorDescription, [ToBccEmails.PayLaterWarning]);
            }

            return Error.Failure($"Payment failed: {ex.StripeError.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create future payment for customer {UserId}", user!.Id);
            return Error.Failure("500", "Failed to create future payment");
        }
    }

    [HttpPost("create-payment-link", Name = "CreatePaymentLink")]
    public async Task<ActionResult<CreatePaymentLinkResponse>> CreatePaymentLink(
        [FromBody] CreatePaymentLinkRequest request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Creating PaymentLink for quote {QuoteId} with payment type {PaymentType}",
                request.QuoteId, request.PaymentType);

            // Get the quote from database
            var quote = await quoteRepository.GetQuoteAsync(request.QuoteId, ct);
            if (quote == null)
            {
                logger.LogWarning("Quote not found: {QuoteId}", request.QuoteId);
                return NotFound("Quote not found");
            }

            // Get customer information
            var customerQuote = await userQuoteRepository.GetUserQuoteByQuoteIdAsync(request.QuoteId, ct);
            if (customerQuote == null)
            {
                logger.LogWarning("Customer quote relationship not found for quote {QuoteId}", request.QuoteId);
                return BadRequest("Customer quote relationship not found");
            }

            var customer = await userRepository.GetUserAsync(customerQuote.UserId, ct);
            if (customer == null)
            {
                logger.LogWarning("Customer not found for quote {QuoteId}", request.QuoteId);
                return BadRequest("Customer not found");
            }

            if (string.IsNullOrEmpty(customer.Email))
            {
                logger.LogWarning("Customer email not found for quote {QuoteId}", request.QuoteId);
                return BadRequest("Customer email is required");
            }

            // Validate quote status - only allow PaymentLinks for pending or partially paid quotes
            if (quote.PaymentStatus != PaymentStatus.Pending && quote.PaymentStatus != PaymentStatus.PartiallyPaid)
            {
                logger.LogWarning(
                    "Quote {QuoteId} is not in a valid state for PaymentLink creation. Current status: {Status}",
                    request.QuoteId, quote.PaymentStatus);
                return BadRequest("Quote must be pending or partially paid to create a PaymentLink");
            }

            // Determine payment amount based on payment type
            decimal paymentAmount = 0;
            string description;

            if (request.Amount.HasValue)
            {
                paymentAmount = request.Amount.Value;
                description = request.Description ?? $"Your Tranzr Moves payment - £{paymentAmount:F2}";
            }
            else
            {
                switch (request.PaymentType)
                {
                    case PaymentType.Full:
                        paymentAmount = quote.TotalCost ?? 0;
                        description = "Your Tranzr Moves payment - Full amount";
                        break;

                    case PaymentType.Deposit:
                        // Calculate deposit amount from percentage
                        var depositPercentage = quote.PaymentType == PaymentType.Deposit ? 25m : 25m; // Default to 25%
                        paymentAmount = (quote.TotalCost ?? 0) * (depositPercentage / 100m);
                        description = $"Your Tranzr Moves payment - {depositPercentage}% deposit";
                        break;

                    case PaymentType.Balance:
                        // Calculate remaining balance
                        var totalCost = quote.TotalCost ?? 0;
                        var paidAmount = quote.PaymentStatus == PaymentStatus.PartiallyPaid
                            ? (quote.DepositAmount ?? 0)
                            : 0;
                        paymentAmount = totalCost - paidAmount;
                        description = "Your Tranzr Moves payment - Remaining balance";
                        break;

                    default:
                        logger.LogWarning("Invalid payment type for PaymentLink: {PaymentType}", request.PaymentType);
                        return BadRequest("Invalid payment type for PaymentLink");
                }
            }

            if (paymentAmount <= 0)
            {
                logger.LogWarning("Invalid payment amount for PaymentLink: {Amount}", paymentAmount);
                return BadRequest("Payment amount must be greater than zero");
            }

            // Create Stripe customer if not exists
            var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
            {
                Query = $"email:'{customer.Email}'",
            }, cancellationToken: ct);

            var stripeCustomer = customerSearchResult.Data.FirstOrDefault();

            if (stripeCustomer == null)
            {
                logger.LogInformation("Creating Stripe customer for {email}", customer.Email);

                var customerOptions = new CustomerCreateOptions
                {
                    Email = customer.Email,
                    Name = customer.FullName,
                    Address = customer.BillingAddress != null
                        ? new AddressOptions
                        {
                            Line1 = customer.BillingAddress.Line1,
                            Line2 = customer.BillingAddress.Line2,
                            City = customer.BillingAddress.City,
                            PostalCode = customer.BillingAddress.PostCode,
                            Country = customer.BillingAddress.Country ?? "United Kingdom",
                            // Enhanced with extended Mapbox fields
                            State = customer.BillingAddress.Region
                        }
                        : null
                };

                _ = await stripeClient.V1.Customers.CreateAsync(customerOptions, cancellationToken: ct);
            }

            // Create PaymentLink
            var paymentLinkOptions = new PaymentLinkCreateOptions
            {
                LineItems =
                [
                    new PaymentLinkLineItemOptions
                    {
                        Price = await CreateOrGetPriceAsync(paymentAmount, description, quote.QuoteReference),
                        Quantity = 1
                    }
                ],
                Metadata = new Dictionary<string, string>
                {
                    { nameof(PaymentMetadata.PaymentType), request.PaymentType.ToString() },
                    { nameof(PaymentMetadata.TotalCost), (quote.TotalCost ?? 0).ToString("F2") },
                    { nameof(PaymentMetadata.DepositPercentage), "25" }, // Default deposit percentage
                    { nameof(PaymentMetadata.DueDate), FormatLocalDateIso(quote.DueDate) },
                    { nameof(PaymentMetadata.QuoteReference), quote.QuoteReference },
                    { nameof(PaymentMetadata.QuoteId), quote.Id.ToString() },
                    { nameof(PaymentMetadata.CustomerEmail), customer.Email },
                    { nameof(PaymentMetadata.PaymentAmount), paymentAmount.ToString("F2") }
                },
                AfterCompletion = new PaymentLinkAfterCompletionOptions
                {
                    Type = "hosted_confirmation",
                    HostedConfirmation = new PaymentLinkAfterCompletionHostedConfirmationOptions
                    {
                        CustomMessage =
                            $"Thank you for your payment! You will receive a confirmation email shortly confirming your payment of £{paymentAmount}."
                    }
                },
                AllowPromotionCodes = false,
                BillingAddressCollection = "auto",
                CustomerCreation = "if_required",
                PaymentIntentData = new PaymentLinkPaymentIntentDataOptions
                {
                    SetupFutureUsage = null, // No future usage for PaymentLinks
                },
                SubmitType = "pay"
            };

            var paymentLink = await stripeClient.V1.PaymentLinks.CreateAsync(paymentLinkOptions, cancellationToken: ct);

            logger.LogInformation("PaymentLink created {PaymentLinkId} for quote {QuoteId} with amount {Amount}",
                paymentLink.Id, request.QuoteId, paymentAmount);

            // Send PaymentLink to customer via email
            bool emailSent = false;
            try
            {
                var templateData = new
                {
                    customerName = customer.FullName,
                    paymentAmount = paymentAmount.ToString("N2"),
                    quoteReference = quote.QuoteReference,
                    paymentLinkUrl = paymentLink.Url,
                    paymentType = request.PaymentType.ToString(),
                    description,
                    currentYear = timeService.NowInUtc().Year
                };

                var subject = $"Payment Link - #{quote.QuoteReference}";
                var htmlEmail = templateService.GenerateEmail("payment-link.html", templateData);
                var textEmail = templateService.GenerateEmail("payment-link.txt", templateData);

                await emailService.SendBookingConfirmationEmailAsync(FromEmails.Booking, subject, customer.Email,
                    htmlEmail, textEmail);
                emailSent = true;

                logger.LogInformation("PaymentLink email sent to {CustomerEmail} for quote {QuoteId}", customer.Email,
                    request.QuoteId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send PaymentLink email to {CustomerEmail} for quote {QuoteId}",
                    customer.Email, request.QuoteId);
                // Don't fail the entire request if email fails
            }

            return new CreatePaymentLinkResponse
            {
                PaymentLinkId = paymentLink.Id,
                PaymentLinkUrl = paymentLink.Url,
                CustomerEmail = customer.Email,
                QuoteReference = quote.QuoteReference,
                Amount = paymentAmount,
                PaymentType = request.PaymentType,
                EmailSent = emailSent
            };
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating PaymentLink for quote {QuoteId}: {ErrorCode} - {ErrorMessage}",
                request.QuoteId, ex.StripeError.Code, ex.StripeError.Message);
            return BadRequest($"PaymentLink creation failed: {ex.StripeError.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create PaymentLink for quote {QuoteId}", request.QuoteId);
            return StatusCode(500, "Failed to create PaymentLink");
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
                logger.LogInformation("A successful setup intent was completed for customer {CustomerId}.",
                    setupIntent.CustomerId);

                await HandleSetupIntentSucceeded(setupIntent);
            }
            else if (stripeEvent.Type == EventTypes.PaymentMethodAttached)
            {
                var paymentMethod = stripeEvent.Data.Object as PaymentMethod;
                // Then define and call a method to handle the successful attachment of a PaymentMethod.
                // handlePaymentMethodAttached(paymentMethod);
            }
            else if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var checkoutSession = stripeEvent.Data.Object as Session;
                logger.LogInformation("A successful PaymentLink payment was completed for PaymentLink {PaymentLinkId}.",
                    checkoutSession.Id);

                await HandleCheckoutSessionCompleted(checkoutSession);
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
            var customer = await stripeClient.V1.Customers.GetAsync(paymentIntent.CustomerId);
            var user = await userRepository.GetUserByEmailAsync(customer.Email, CancellationToken.None);

            if (!string.IsNullOrEmpty(customer.Email))
            {
                var orderInstant = timeService.Now();

                var hasPaymentType =
                    paymentIntent.Metadata.TryGetValue(nameof(PaymentMetadata.PaymentType), out var paymentType);

                if (!hasPaymentType)
                {
                    return;
                }

                // Get quote by QuoteReference from metadata if available
                var quoteReference =
                    paymentIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.QuoteReference), "");

                if (string.IsNullOrEmpty(quoteReference))
                {
                    logger.LogWarning("No quote reference found in metadata for payment intent {PaymentIntentId}",
                        paymentIntent.Id);
                    return;
                }

                // Retrieve the quote from your database
                var quote = await quoteRepository.GetQuoteByReferenceAsync(quoteReference);

                var userMapper = new UserMapper();
                var mapper = new QuoteMapper();
                var quoteDto = mapper.ToDto(quote);

                if (paymentType == nameof(PaymentType.Balance) && quote!.PaymentStatus == PaymentStatus.PartiallyPaid)
                {
                    decimal? extraCharges = null;
                    var extraChargesDescription = string.Empty;

                    // Check if there's extra charges metadata
                    if (paymentIntent.Metadata.TryGetValue(nameof(PaymentMetadata.ExtraCharges),
                            out var extraChargesStr) &&
                        decimal.TryParse(extraChargesStr, out var extraCharge) && extraCharge > 0)
                    {
                        extraCharges = extraCharge;
                        extraChargesDescription =
                            paymentIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.ExtraChargesDescription),
                                "Additional charges");

                        // Update quote with extra charges
                        quoteDto.Pricing!.TotalCost = quote.DepositAmount + paymentIntent.Amount / 100.0m;
                        logger.LogInformation(
                            "Added extra charges of {ExtraCharges} to quote {QuoteReference} for {chargesDescription}",
                            extraCharges, quoteReference, extraChargesDescription);
                    }

                    // Retrieve the latest charge
                    var charge = await stripeClient.V1.Charges.GetAsync(paymentIntent.LatestChargeId);

                    // Create QuoteAdditionalPayment record
                    var additionalPayment = new QuoteAdditionalPaymentDto
                    {
                        QuoteId = quote.Id,
                        Amount = paymentIntent.Amount / 100.0m,
                        Description = $"Balance Payment for quote - #{quoteReference}",
                        PaymentMethodId = paymentIntent.PaymentMethodId,
                        PaymentIntentId = paymentIntent.Id,
                        ReceiptUrl = charge.ReceiptUrl
                    };

                    // Add to quote's additional payments collection (preserve existing payments)
                    if (quoteDto.QuoteAdditionalPayments is null)
                    {
                        quoteDto.QuoteAdditionalPayments = [];
                        logger.LogInformation(
                            "Created new QuoteAdditionalPayments collection for quote {QuoteReference}",
                            quoteReference);
                    }
                    else
                    {
                        logger.LogInformation(
                            "Adding payment to existing QuoteAdditionalPayments collection for quote {QuoteReference}. Current count: {Count}",
                            quoteReference, quoteDto.QuoteAdditionalPayments.Count);
                    }

                    quoteDto.QuoteAdditionalPayments.Add(additionalPayment);

                    quoteDto.Payment!.Status = PaymentStatus.Paid;
                    await UpdateQuoteAsync(new SaveQuoteRequest
                    {
                        Quote = quoteDto,
                        Customer = userMapper.ToDto(user!)
                    });

                    var totalCost = (quoteDto.Pricing?.TotalCost!).Value;

                    var templateData = new
                    {
                        customerName = user?.FullName,
                        balanceAmount = (paymentIntent.Amount / 100.0m).ToString("N2"),
                        totalAmount = totalCost.ToString("N2"),
                        quoteReference,
                        paymentDate = FormatInstantAsEmailDate(orderInstant),
                        paymentTime = FormatInstantAsEmailTimeUtc(orderInstant),
                        currentYear = timeService.NowInUtc().Year,
                        // Include extra charges details in the email when applicable
                        extraCharges = extraCharges is > 0
                            ? extraCharges.Value.ToString("N2", CultureInfo.InvariantCulture)
                            : null,
                        extraChargesDescription = extraCharges is > 0 ? extraChargesDescription : null
                    };

                    var subject = $"Balance Payment Confirmation - #{quoteReference}";
                    var htmlEmail = templateService.GenerateEmail("balance-confirmation.html", templateData);
                    var textEmail = templateService.GenerateEmail("balance-confirmation.txt", templateData);

                    await emailService.SendBookingConfirmationEmailAsync(FromEmails.Booking, subject, customer.Email,
                        htmlEmail, textEmail);

                    logger.LogInformation(
                        "Balance payment confirmation email sent for payment intent {PaymentIntentId}",
                        paymentIntent.Id);
                }
                else if (paymentType == nameof(PaymentType.Deposit))
                {
                    // Send deposit confirmation email
                    logger.LogInformation("Sending deposit confirmation email for payment intent {PaymentIntentId}",
                        paymentIntent.Id);

                    // Retrieve the latest charge
                    var charge = await stripeClient.V1.Charges.GetAsync(paymentIntent.LatestChargeId);

                    quoteDto.Payment!.ReceiptUrl = charge.ReceiptUrl;
                    quoteDto.Payment.PaymentMethodId = paymentIntent.PaymentMethodId;

                    quoteDto.Payment!.Status = PaymentStatus.PartiallyPaid;

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
                        paymentDate = FormatInstantAsEmailDate(orderInstant),
                        paymentTime = FormatInstantAsEmailTimeUtc(orderInstant),
                        currentYear = timeService.NowInUtc().Year
                    };

                    var subject = $"Deposit Confirmation - #{quoteReference}";
                    var htmlEmail = templateService.GenerateEmail("deposit-confirmation.html", templateData);
                    var textEmail = templateService.GenerateEmail("deposit-confirmation.txt", templateData);

                    await emailService.SendBookingConfirmationEmailAsync(FromEmails.Booking, subject, customer.Email,
                        htmlEmail, textEmail);

                    logger.LogInformation("Deposit confirmation email sent for payment intent {PaymentIntentId}",
                        paymentIntent.Id);
                }
                else
                {
                    // Retrieve the latest charge
                    var charge = await stripeClient.V1.Charges.GetAsync(paymentIntent.LatestChargeId);

                    quoteDto.Payment!.ReceiptUrl = charge.ReceiptUrl;
                    quoteDto.Payment.PaymentMethodId = paymentIntent.PaymentMethodId;

                    quoteDto.Payment!.Status = PaymentStatus.Paid;
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
                        paymentDate = FormatInstantAsEmailDate(orderInstant),
                        paymentTime = FormatInstantAsEmailTimeUtc(orderInstant),
                        currentYear = timeService.NowInUtc().Year
                    };

                    var subject = $"Order Confirmation - #{quoteReference}";
                    var htmlEmail = templateService.GenerateEmail("full-payment-confirmation.html", templateData);
                    var textEmail = templateService.GenerateEmail("full-payment-confirmation.txt", templateData);

                    await emailService.SendBookingConfirmationEmailAsync(FromEmails.Booking, subject, customer.Email,
                        htmlEmail, textEmail);

                    logger.LogInformation("Order confirmation email sent for payment intent {PaymentIntentId}",
                        paymentIntent.Id);
                }
            }
            else
            {
                logger.LogWarning(
                    "Could not send confirmation email - customer email not found for payment intent {PaymentIntentId}",
                    paymentIntent.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email for payment intent {PaymentIntentId}",
                paymentIntent.Id);
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
                var setupInstant = timeService.Now();

                // Get quote by QuoteReference from metadata if available
                var quoteReference = setupIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.QuoteReference), "");
                var quoteId = setupIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.QuoteId), "");

                if (string.IsNullOrEmpty(quoteReference))
                {
                    logger.LogWarning("No quote reference found in metadata for payment intent {PaymentIntentId}",
                        setupIntent.Id);
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
                    logger.LogInformation("Sending setup confirmation email for setup intent {SetupIntentId}",
                        setupIntent.Id);

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
                        setupDate = FormatInstantAsEmailDate(setupInstant),
                        setupTime = FormatInstantAsEmailTimeUtc(setupInstant),
                        currentYear = timeService.NowInUtc().Year
                    };

                    var subject = $"Payment Setup Confirmation - #{quoteReference}";
                    var htmlEmail = templateService.GenerateEmail("setup-confirmation.html", templateData);
                    var textEmail = templateService.GenerateEmail("setup-confirmation.txt", templateData);

                    await emailService.SendBookingConfirmationEmailAsync(FromEmails.Booking, subject, customer.Email,
                        htmlEmail, textEmail);

                    logger.LogInformation("Setup confirmation email sent for setup intent {SetupIntentId}",
                        setupIntent.Id);
                }
            }
            else
            {
                logger.LogWarning(
                    "Could not send setup confirmation email - customer email not found for setup intent {SetupIntentId}",
                    setupIntent.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send setup confirmation email for setup intent {SetupIntentId}",
                setupIntent.Id);
            // Don't throw here to avoid failing the webhook
        }
    }

    private async Task HandleCheckoutSessionCompleted(Session session)
    {
        try
        {
            // Get quote by QuoteReference from metadata if available
            var quoteReference = session.Metadata.GetValueOrDefault(nameof(PaymentMetadata.QuoteReference), "");
            var customerEmail = session.Metadata.GetValueOrDefault(nameof(PaymentMetadata.CustomerEmail), "");

            if (string.IsNullOrEmpty(quoteReference))
            {
                logger.LogWarning("No quote reference found in metadata for Checkout Session {SessionId}", session.Id);
                return;
            }

            if (string.IsNullOrEmpty(customerEmail))
            {
                logger.LogWarning("No customer email found in metadata for Checkout Session {SessionId}", session.Id);
                return;
            }

            var user = await userRepository.GetUserByEmailAsync(customerEmail, CancellationToken.None);

            if (user is not null)
            {
                var paymentInstant = timeService.Now();

                // Retrieve the quote from your database
                var quote = await quoteRepository.GetQuoteByReferenceAsync(quoteReference);

                if (quote is null)
                {
                    logger.LogWarning("Quote not found for reference {QuoteReference} in Checkout Session {SessionId}",
                        quoteReference, session.Id);
                    return;
                }

                var userMapper = new UserMapper();
                var mapper = new QuoteMapper();
                var quoteDto = mapper.ToDto(quote);

                // Get payment amount from metadata or calculate from line items
                decimal paymentAmount;
                if (session.Metadata.TryGetValue(nameof(PaymentMetadata.PaymentAmount), out var amountStr) &&
                    decimal.TryParse(amountStr, out var amount))
                {
                    paymentAmount = amount;
                }
                else
                {
                    // Fallback: calculate from line items if available
                    paymentAmount = 0m;
                    if (session.LineItems?.Data != null)
                    {
                        foreach (var item in session.LineItems.Data)
                        {
                            paymentAmount += item.AmountTotal / 100.0m;
                        }
                    }
                }

                // Get receipt URL and payment method from payment intent if available
                string? receiptUrl = null;
                string? paymentMethodId = null;
                if (!string.IsNullOrEmpty(session.PaymentIntentId))
                {
                    try
                    {
                        var paymentIntent = await stripeClient.V1.PaymentIntents.GetAsync(session.PaymentIntentId);
                        paymentMethodId = paymentIntent.PaymentMethodId;
                        if (!string.IsNullOrEmpty(paymentIntent.LatestChargeId))
                        {
                            var charge = await stripeClient.V1.Charges.GetAsync(paymentIntent.LatestChargeId);
                            receiptUrl = charge.ReceiptUrl;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to retrieve payment details for Checkout Session {SessionId}",
                            session.Id);
                    }
                }

                // Create QuoteAdditionalPayment record

                string additionalPaymentDescription;

                if (session.Metadata.TryGetValue(nameof(PaymentMetadata.ExtraChargesDescription), out var description))
                {
                    additionalPaymentDescription = description + $" for quote #{quoteReference}";
                }
                else
                {
                    additionalPaymentDescription = $"Payment for quote #{quoteReference}";
                }

                var additionalPayment = new QuoteAdditionalPaymentDto
                {
                    QuoteId = quote.Id,
                    Amount = paymentAmount,
                    Description = additionalPaymentDescription,
                    PaymentMethodId = paymentMethodId,
                    PaymentIntentId = session.PaymentIntentId,
                    ReceiptUrl = receiptUrl
                };

                // Check for duplicate payments (same PaymentIntentId)
                if (!string.IsNullOrEmpty(session.PaymentIntentId) && quoteDto.QuoteAdditionalPayments != null)
                {
                    var existingPayment =
                        quoteDto.QuoteAdditionalPayments.FirstOrDefault(p =>
                            p.PaymentIntentId == session.PaymentIntentId);
                    if (existingPayment != null)
                    {
                        logger.LogWarning(
                            "Payment with PaymentIntentId {PaymentIntentId} already exists for quote {QuoteReference}. Skipping duplicate payment.",
                            session.PaymentIntentId, quoteReference);
                        return; // Exit early to prevent duplicate processing
                    }
                }

                // Add to quote's additional payments collection (preserve existing payments)
                if (quoteDto.QuoteAdditionalPayments is null)
                {
                    quoteDto.QuoteAdditionalPayments = [];
                    logger.LogInformation("Created new QuoteAdditionalPayments collection for quote {QuoteReference}",
                        quoteReference);
                }
                else
                {
                    logger.LogInformation(
                        "Adding payment to existing QuoteAdditionalPayments collection for quote {QuoteReference}. Current count: {Count}",
                        quoteReference, quoteDto.QuoteAdditionalPayments.Count);
                }

                quoteDto.QuoteAdditionalPayments.Add(additionalPayment);

                logger.LogInformation(
                    "Added payment {PaymentAmount} to QuoteAdditionalPayments for quote {QuoteReference}. New count: {Count}",
                    paymentAmount, quoteReference, quoteDto.QuoteAdditionalPayments.Count);

                // Update quote status to reflect payment
                quoteDto.Payment!.Status =
                    PaymentStatus.Paid; //TODO: Check if this could be problematic for multiple payments

                await UpdateQuoteAsync(new SaveQuoteRequest
                {
                    Quote = quoteDto,
                    Customer = userMapper.ToDto(user)
                });

                // Send generic confirmation email
                var templateData = new
                {
                    customerName = user.FullName,
                    paymentAmount = paymentAmount.ToString("N2"),
                    quoteReference,
                    paymentDate = FormatInstantAsEmailDate(paymentInstant),
                    paymentTime = FormatInstantAsEmailTimeUtc(paymentInstant),
                    description = additionalPaymentDescription,
                    receiptUrl = receiptUrl,
                    currentYear = timeService.NowInUtc().Year
                };

                var subject = $"Payment Confirmation - #{quoteReference}";
                var htmlEmail = templateService.GenerateEmail("checkout-session-completed.html", templateData);
                var textEmail = templateService.GenerateEmail("checkout-session-completed.txt", templateData);

                await emailService.SendBookingConfirmationEmailAsync(FromEmails.Booking, subject, customerEmail,
                    htmlEmail, textEmail);

                logger.LogInformation(
                    "Checkout session payment confirmation email sent for Session {SessionId}, Amount: £{Amount}",
                    session.Id, paymentAmount);
            }
            else
            {
                logger.LogWarning("Could not find user with email {CustomerEmail} for Checkout Session {SessionId}",
                    customerEmail, session.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Checkout Session completion {SessionId}", session.Id);
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
                currentYear = timeService.NowInUtc().Year
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
