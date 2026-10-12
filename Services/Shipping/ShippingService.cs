using Healthcare_Industry.Models;

namespace Healthcare_Industry.Services;

public interface IShippingService
{
    Task<string> CreateShipmentAsync(Order order);
}

public sealed class DemoShippingService : IShippingService
{
    public Task<string> CreateShipmentAsync(Order order)
    {
        var trackingNumber = $"WN{DateTime.UtcNow:yyMMdd}{order.Id:000000}";
        return Task.FromResult(trackingNumber);
    }
}
