using Healthcare_Industry.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Healthcare_Industry.Services;

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = "";
    public string Currency { get; set; } = "myr";
}

public sealed class StripeCheckoutPaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;

    public StripeCheckoutPaymentGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
    }

    public async Task<PaymentRequest> CreatePaymentRequestAsync(Order order, string returnUrl)
    {
        StripeConfiguration.ApiKey = _options.SecretKey;

        var successUrl = BuildReturnUrl(returnUrl, order.Id, "{CHECKOUT_SESSION_ID}", "success");
        var cancelUrl = BuildReturnUrl(returnUrl, order.Id, "{CHECKOUT_SESSION_ID}", "cancel");

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            CustomerEmail = order.CustomerEmail,
            ClientReferenceId = order.Id.ToString(),
            PaymentMethodTypes = GetPaymentMethodTypes(order),
            Metadata = new Dictionary<string, string>
            {
                ["order_id"] = order.Id.ToString()
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = _options.Currency,
                        UnitAmount = ToStripeAmount(order.Total),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"WellNest Order #{order.Id}",
                            Description = $"{order.Items.Count} item(s)"
                        }
                    }
                }
            ]
        };

        Session session;
        try
        {
            var service = new SessionService();
            session = await service.CreateAsync(sessionOptions);
        }
        catch (StripeException ex)
        {
            throw new InvalidOperationException("Stripe payment gateway is not configured with a valid API key. Use a real Stripe secret key or leave the demo placeholder in appsettings.json to use the demo gateway.", ex);
        }

        return new PaymentRequest
        {
            Reference = session.Id,
            RedirectUrl = session.Url,
            Amount = order.Total
        };
    }

    public async Task<PaymentVerification> VerifyPaymentAsync(Order order, string reference, bool isSuccess)
    {
        StripeConfiguration.ApiKey = _options.SecretKey;

        var service = new SessionService();
        var session = await service.GetAsync(reference);

        var belongsToOrder = session.ClientReferenceId == order.Id.ToString()
            || (session.Metadata is not null
                && session.Metadata.TryGetValue("order_id", out var orderId)
                && orderId == order.Id.ToString());

        if (!belongsToOrder)
        {
            return new PaymentVerification
            {
                IsSuccessful = false,
                ProviderTransactionId = "",
                Message = "Stripe session does not match this order."
            };
        }

        var paid = string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase);

        return new PaymentVerification
        {
            IsSuccessful = paid,
            ProviderTransactionId = session.PaymentIntentId ?? session.Id,
            Message = paid ? "Payment verified by Stripe Checkout." : $"Stripe payment status: {session.PaymentStatus}."
        };
    }

    private static string BuildReturnUrl(string returnUrl, int orderId, string sessionId, string result)
    {
        var separator = returnUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{returnUrl}{separator}orderId={orderId}&sessionId={sessionId}&result={result}";
    }

    public static List<string> GetPaymentMethodTypes(Order order)
        => order.PaymentMethod switch
        {
            "GatewayBanking" => ["fpx"],
            "GatewayWallet" => ["grabpay"],
            _ => ["card"]
        };

    private static long ToStripeAmount(decimal amount)
        => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
