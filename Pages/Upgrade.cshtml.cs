using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using comp4870project.Model;
public class PaymentModel : PageModel
{
    private readonly StripePaymentService _stripePaymentService;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<User> _userManager; // Assuming User is your user class

    public PaymentModel(StripePaymentService stripePaymentService, IWebHostEnvironment environment, UserManager<User> userManager)
    {
        _stripePaymentService = stripePaymentService;
        _environment = environment;
        _userManager = userManager; // Initialize UserManager
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

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Error"); // Or handle the error as you see fit
        }

        string successUrl;
        string cancelUrl;
        // if (_environment.IsDevelopment())
        // {
        //     successUrl = "http://localhost:5144/"; // Local development success URL
        //     cancelUrl = "http://localhost:5144/pricing"; // Local development cancel URL
        // }
        // else

        successUrl = "https://chatlingo2.azurewebsites.net"; // Production success URL
        cancelUrl = "https://chatlingo2.azurewebsites.net/pricing"; // Production cancel URL



        var session = await _stripePaymentService.CreateCheckoutSessionAsync(items, successUrl, cancelUrl);


        user.IsSubscribed = true;

        var result = await _userManager.UpdateAsync(user);


        if (!result.Succeeded)
        {
            return RedirectToPage("/Error");
        }

        // Retrieve the updated user information
        var updatedUser = await _userManager.FindByIdAsync(user.Id);
        if (updatedUser != null)
        {
            // Log or display the updated information
            // For example, logging the subscription status
            Console.WriteLine($"User {updatedUser.UserName} subscription status: {updatedUser.IsSubscribed}");
        }

        return Redirect(session.Url);
    }
}


