using Microsoft.AspNetCore.Mvc;
using Stripe;
using TranzrMoves.Api.Dtos;

namespace TranzrMoves.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CheckoutController(StripeClient stripeClient, IConfiguration configuration, ILogger<CheckoutController> logger) : ControllerBase
{
    [HttpPost("payment-sheet", Name = "CreateStripeIntent")]
    public async Task<ActionResult<PaymentSheetCreateResponse>> CreatePaymentSheet([FromBody] PaymentSheetRequest paymentSheetRequest)
    {

        // Use an existing Customer ID if this is a returning customer.
        logger.LogInformation("Creating payment sheet");
        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{paymentSheetRequest.Email}'",
        });
        
        var customer = customerSearchResult.Data.FirstOrDefault();
        
        if (customer is null)
        { 
            logger.LogInformation("Customer does not exist. Creating customer with {email}",  paymentSheetRequest.Email);
            var customerOptions = new CustomerCreateOptions
            {
                Email = paymentSheetRequest.Email,
                Name = paymentSheetRequest.Name
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
            Amount = paymentSheetRequest.Amount * 100, // Convert to pence
            Currency = "gbp",
            Customer = customer.Id,
            Description = "Your Tranzr Moves payment",
            ReceiptEmail = paymentSheetRequest.Email,
            
            // In the latest version of the API, specifying the `automatic_payment_methods` parameter
            // is optional because Stripe enables its functionality by default.
            
            // AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            // {
            //     Enabled = true,
            //     AllowRedirects = "always", // This allows redirect-based payment methods
            // },
            
            PaymentMethodTypes = [
                "card",
                "google_pay",
                "afterpay_clearpay",
                "klarna", 
                "amazon_pay",
                "paypal",
                "revolut_pay",
                "apple_pay",
            ],
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
                logger.LogInformation("A successful payment for {paymanetAmount} GBP was made.", paymentIntent.Amount);
                // Then define and call a method to handle the successful payment intent.
                // handlePaymentIntentSucceeded(paymentIntent);
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
}