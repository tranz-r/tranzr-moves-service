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

        var paymentIntentOptions = new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(paymentSheetRequest.Cost.Total * 100, 0, MidpointRounding.AwayFromZero), // Convert to pence
            Currency = "gbp",
            Customer = customer.Id,
            Description = "Your Tranzr Moves payment",
            ReceiptEmail = paymentSheetRequest.Customer.Email,
            
            
            // In the latest version of the API, specifying the `automatic_payment_methods` parameter
            // is optional because Stripe enables its functionality by default.
            
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "always", // This allows redirect-based payment methods
            }
        };
        
        logger.LogInformation("Payment intent created");
        
        var paymentIntent = await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions);

        return new PaymentSheetCreateResponse
        {
            PaymentIntent = paymentIntent.ClientSecret,
            EphemeralKey = ephemeralKey.Secret,
            Customer = customer.Id,
            PublishableKey = StripeConfiguration.ClientId,
        };
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
                
                // Send order confirmation email
                await awsEmailService.SendOrderConfirmationEmailAsync(
                    customer.Email,
                    customerName,
                    paymentIntent.Amount,
                    orderId,
                    orderDate
                );
                
                logger.LogInformation("Order confirmation email sent for payment intent {PaymentIntentId}", paymentIntent.Id);
            }
            else
            {
                logger.LogWarning("Could not send order confirmation email - customer email not found for payment intent {PaymentIntentId}", paymentIntent.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order confirmation email for payment intent {PaymentIntentId}", paymentIntent.Id);
            // Don't throw here to avoid failing the webhook
        }
    }
}