using Healthcare_Industry.Models;

namespace Healthcare_Industry.Services;

public sealed class PaymentRequest
{
    public required string Reference { get; init; }
    public required string RedirectUrl { get; init; }
    public required decimal Amount { get; init; }
}

public sealed class PaymentVerification
{
    public required bool IsSuccessful { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required string Message { get; init; }
}

public interface IPaymentGateway
{
    Task<PaymentRequest> CreatePaymentRequestAsync(Order order, string returnUrl);
    Task<PaymentVerification> VerifyPaymentAsync(Order order, string reference, bool isSuccess);
}

public sealed class DemoPaymentGateway : IPaymentGateway
{
    public Task<PaymentRequest> CreatePaymentRequestAsync(Order order, string returnUrl)
    {
        var reference = $"DEMO-{order.Id}";

        return Task.FromResult(new PaymentRequest
        {
            Reference = reference,
            Amount = order.Total,
            RedirectUrl = BuildDemoGatewayUrl(returnUrl, order.Id, reference)
        });
    }

    public Task<PaymentVerification> VerifyPaymentAsync(Order order, string reference, bool isSuccess)
    {
        var expected = $"DEMO-{order.Id}";
        if (!string.Equals(reference, expected, StringComparison.Ordinal))
        {
            return Task.FromResult(new PaymentVerification
            {
                IsSuccessful = false,
                ProviderTransactionId = "",
                Message = "Payment reference does not match the order."
            });
        }

        return Task.FromResult(new PaymentVerification
        {
            IsSuccessful = isSuccess,
            ProviderTransactionId = isSuccess ? $"TXN-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}" : "",
            Message = isSuccess ? "Payment verified by demo gateway." : "Payment was declined by demo gateway."
        });
    }

    private static string BuildDemoGatewayUrl(string returnUrl, int orderId, string reference)
    {
        var query = $"orderId={orderId}&reference={Uri.EscapeDataString(reference)}";
        return Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)
            ? $"{uri.Scheme}://{uri.Authority}/Customer/PaymentGateway?{query}"
            : $"/Customer/PaymentGateway?{query}";
    }
}
