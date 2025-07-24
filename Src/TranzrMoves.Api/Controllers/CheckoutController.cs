using Microsoft.AspNetCore.Mvc;
using Stripe;
using TranzrMoves.Api.Dtos;

namespace TranzrMoves.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CheckoutController(StripeClient stripeClient, ILogger<CheckoutController> logger) : ControllerBase
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
            // In the latest version of the API, specifying the `automatic_payment_methods` parameter
            // is optional because Stripe enables its functionality by default.
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "always", // This allows redirect-based payment methods
            },
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
}