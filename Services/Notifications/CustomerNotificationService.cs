using Healthcare_Industry.Models;

namespace Healthcare_Industry.Services;

public interface ICustomerNotificationService
{
    Task SendOrderPaidAsync(Order order);
    Task SendOrderStatusChangedAsync(Order order);
}

public sealed class FileCustomerNotificationService : ICustomerNotificationService
{
    private readonly IWebHostEnvironment _environment;

    public FileCustomerNotificationService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task SendOrderPaidAsync(Order order)
        => WriteAsync(order, "Order payment confirmed", $"Payment for order #{order.Id} was received.");

    public Task SendOrderStatusChangedAsync(Order order)
        => WriteAsync(order, "Order status updated", $"Order #{order.Id} is now {order.Status}.");

    private async Task WriteAsync(Order order, string subject, string body)
    {
        var folder = Path.Combine(_environment.WebRootPath, "cloud-notifications");
        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, $"order-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
        var content = $"""
        To: {order.CustomerEmail}
        Subject: {subject}
        Provider: Demo cloud email provider
        CreatedUtc: {DateTime.UtcNow:O}

        Hi {order.CustomerName},

        {body}

        Delivery address:
        {order.DeliveryAddress}

        Total: RM {order.Total:0.00}
        """;

        await File.WriteAllTextAsync(path, content);
    }
}
