using Healthcare_Industry.Models;

namespace Healthcare_Industry.Services;

public sealed class StockAvailabilityResult
{
    public bool IsAvailable => Messages.Count == 0;
    public List<string> Messages { get; } = new();
}

public static class StockAvailabilityService
{
    public static StockAvailabilityResult Validate(IEnumerable<CartItem> items)
    {
        var result = new StockAvailabilityResult();

        foreach (var item in items)
        {
            if (item.Product is null)
            {
                result.Messages.Add("One selected item is no longer available.");
                continue;
            }

            if (item.Product.Stock <= 0)
            {
                result.Messages.Add($"{item.Product.Name} is out of stock.");
                continue;
            }

            if (item.Quantity > item.Product.Stock)
            {
                result.Messages.Add($"{item.Product.Name} only has {item.Product.Stock} in stock.");
            }
        }

        return result;
    }
}
