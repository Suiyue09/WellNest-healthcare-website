using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer;

public class HistoryModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public HistoryModel(ApplicationDbContext db) => _db = db;

    public List<Order> Orders { get; set; } = new();
    public string Status { get; set; } = "All";
    public int TotalOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public string CustomerInitial { get; set; } = "U";
    public HashSet<int> ReviewedOrderIds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
            return RedirectToPage("/Account/Login");

        Status = string.IsNullOrWhiteSpace(status) ? "All" : status;

        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.FullName, u.Email })
            .FirstOrDefaultAsync();
        CustomerInitial = CustomerDisplayService.GetInitial(user?.FullName, user?.Email);

        var allOrders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        TotalOrders = allOrders.Count;
        ActiveOrders = allOrders.Count(o => o.Status is OrderStatus.Pending or OrderStatus.Packed or OrderStatus.Shipped);
        DeliveredOrders = allOrders.Count(o => o.Status == OrderStatus.Delivered);
        TotalSpent = allOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total);

        Orders = Enum.TryParse<OrderStatus>(Status, out var parsed)
            ? allOrders.Where(o => o.Status == parsed).ToList()
            : allOrders;

        var deliveredOrders = allOrders
            .Where(o => o.Status == OrderStatus.Delivered)
            .ToList();
        var deliveredOrderIds = deliveredOrders.Select(o => o.Id).ToList();
        var reviewedProductsByOrder = await _db.ProductReviews
            .AsNoTracking()
            .Where(r => r.CustomerId == userId && deliveredOrderIds.Contains(r.OrderId))
            .GroupBy(r => r.OrderId)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Select(r => r.ProductId).ToHashSet());

        ReviewedOrderIds = deliveredOrders
            .Where(order =>
            {
                var productIds = order.Items.Select(item => item.ProductId).Distinct();
                return reviewedProductsByOrder.TryGetValue(order.Id, out var reviewedProductIds)
                    && productIds.All(reviewedProductIds.Contains);
            })
            .Select(order => order.Id)
            .ToHashSet();

        return Page();
    }
}
