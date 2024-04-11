using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

public class PaymentModel : PageModel
{
    private readonly StripePaymentService _stripePaymentService;
    private readonly IWebHostEnvironment _environment;
    public PaymentModel(StripePaymentService stripePaymentService, IWebHostEnvironment environment)
    {
        _stripePaymentService = stripePaymentService;
        _environment = environment;
    }

    public async Task<IActionResult> OnGetCreateCheckoutSessionAsync()
    {
        var items = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = 99900, // Price in cents
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "ChatPro Subscription",
                    },
                },
                Quantity = 1,
            },
        };

        string successUrl;
        string cancelUrl;
        if (_environment.IsDevelopment())
        {
            successUrl = "http://localhost:5144/"; // Local development success URL
            cancelUrl = "http://localhost:5144/pricing"; // Local development cancel URL
        }
        else
        {
            successUrl = "https://chatlingo2.azurewebsites.net"; // Production success URL
            cancelUrl = "https://chatlingo2.azurewebsites.net/pricing"; // Production cancel URL
        }


        var session = await _stripePaymentService.CreateCheckoutSessionAsync(items, successUrl, cancelUrl);

        return Redirect(session.Url);
    }
}


