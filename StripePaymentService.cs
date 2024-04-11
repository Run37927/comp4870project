using Stripe;
using Stripe.Checkout;
using System.Threading.Tasks;

public class StripePaymentService
{
    public async Task<Session> CreateCheckoutSessionAsync(List<SessionLineItemOptions> items, string successUrl, string cancelUrl)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string>
        {
            "card",
        },
            LineItems = items,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Mode = "payment",
        };

        var service = new SessionService();
        Session session = await service.CreateAsync(options);
        return session;
    }

}
