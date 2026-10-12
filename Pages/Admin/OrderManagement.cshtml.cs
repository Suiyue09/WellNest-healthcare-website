using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Healthcare_Industry.Pages.Admin;
public class OrderManagementModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICustomerNotificationService _notificationService;
    public OrderManagementModel(ApplicationDbContext db, ICustomerNotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }
    public List<Order> Orders { get; set; } = new();
    public string? StatusFilter { get; set; }

    // Maps AssignedDeliveryId -> (Rider Name, Rider Phone)
    public Dictionary<string, (string Name, string? Phone)> RiderInfo { get; set; } = new();

    public string? Message { get; set; }
    public bool IsSuccess { get; set; }

    // summary stats (across all orders)
    public int TotalOrders { get; set; }
    public int PendingCount { get; set; }
    public int DeliveredCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public async Task OnGetAsync(string? status)
    {
        StatusFilter = status;
        TotalOrders = await _db.Orders.CountAsync();
        PendingCount = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        DeliveredCount = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
        TotalRevenue = await _db.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total) ?? 0m;
        var query = _db.Orders.Include(o => o.Items).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var st))
            query = query.Where(o => o.Status == st);
        Orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

        // Load rider details for any assigned orders
        var riderIds = Orders
            .Where(o => !string.IsNullOrEmpty(o.AssignedDeliveryId))
            .Select(o => o.AssignedDeliveryId!)
            .Distinct()
            .ToList();

        if (riderIds.Any())
        {
            var riders = await _db.Users
                .Where(u => riderIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.PhoneNumber })
                .ToListAsync();

            RiderInfo = riders.ToDictionary(
                r => r.Id,
                r => (r.FullName, r.PhoneNumber)
            );
        }
    }
    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string status, string? filter)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is not null && Enum.TryParse<OrderStatus>(status, out var st))
        {
            // Delivered can only be set through the rider's UpdateStatus flow,
            // since that's the only place DeliveryEarning gets calculated.
            if (st == OrderStatus.Delivered && order.Status != OrderStatus.Delivered)
            {
                TempData["StatusError"] = $"Order #{order.Id} cannot be marked Delivered from here — this must be done by the assigned rider through the Delivery app, so earnings are calculated correctly.";
                return RedirectToPage(new { status = filter });
            }

            order.Status = st;
            await _db.SaveChangesAsync();
            await _notificationService.SendOrderStatusChangedAsync(order);
        }
        return RedirectToPage(new { status = filter });
    }
}