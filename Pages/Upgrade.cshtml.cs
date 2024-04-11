using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PaymentModel : PageModel
{
    private readonly StripePaymentService _stripePaymentService;

    public PaymentModel(StripePaymentService stripePaymentService)
    {
        _stripePaymentService = stripePaymentService;
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

        var successUrl = "http://localhost:5144/chat"; // Replace with your actual success URL
        var cancelUrl = "http://localhost:5144/pricing"; // Replace with your actual cancel URL

        var session = await _stripePaymentService.CreateCheckoutSessionAsync(items, successUrl, cancelUrl);

        return Redirect(session.Url);
    }
}
