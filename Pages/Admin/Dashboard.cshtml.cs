using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_Industry.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public DashboardModel(ApplicationDbContext db) => _db = db;

    // Stat cards
    public int TotalProducts { get; set; }
    public int LowStock { get; set; }
    public int OutOfStock { get; set; }
    public int TotalUsers { get; set; }

    // Small recent-products table
    public List<Product> RecentProducts { get; set; } = new();

    // Sales overview (same names used in SalesDashboard)
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AvgOrderValue { get; set; }
    public int CancelledCount { get; set; }
    public decimal PendingCodAmount { get; set; }
    public List<string> TrendLabels { get; set; } = new();
    public List<decimal> TrendValues { get; set; } = new();
    public List<(string Name, int Qty, decimal Revenue)> TopProducts { get; set; } = new();
    public List<(string Category, decimal Revenue)> CategoryBreakdown { get; set; } = new();
    public List<(string Status, int Count)> StatusBreakdown { get; set; } = new();

    private static bool IsRevenueRecognized(Order o)
    {
        if (o.Status == OrderStatus.Cancelled) return false;
        if (o.CodAmount > 0) return o.IsCodCollected;
        return o.PaymentStatus == PaymentStatus.Paid;
    }

    public async Task OnGetAsync()
    {
        TotalProducts = await _db.Products.CountAsync();
        LowStock = await _db.Products.CountAsync(p => p.Stock > 0 && p.Stock < 10);
        OutOfStock = await _db.Products.CountAsync(p => p.Stock == 0);
        TotalUsers = await _db.Users.CountAsync();

        RecentProducts = await _db.Products
            .OrderByDescending(p => p.Id)
            .Take(5)
            .ToListAsync();

        var allOrders = await _db.Orders.Include(o => o.Items).ToListAsync();
        var validOrders = allOrders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var revenueOrders = allOrders.Where(IsRevenueRecognized).ToList();

        TotalRevenue = revenueOrders.Sum(o => o.Total);
        TotalOrders = allOrders.Count;
        AvgOrderValue = revenueOrders.Count > 0 ? Math.Round(revenueOrders.Average(o => o.Total), 2) : 0;
        CancelledCount = allOrders.Count(o => o.Status == OrderStatus.Cancelled);

        PendingCodAmount = validOrders
            .Where(o => o.CodAmount > 0 && !o.IsCodCollected)
            .Sum(o => o.CodAmount);

        var today = DateTime.UtcNow.Date;
        for (int i = 13; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var dayTotal = revenueOrders
                .Where(o => o.OrderDate.Date == day)
                .Sum(o => o.Total);

            TrendLabels.Add(day.ToString("d MMM"));
            TrendValues.Add(dayTotal);
        }

        TopProducts = revenueOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductName)
            .Select(g => (g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.Quantity * i.UnitPrice)))
            .OrderByDescending(x => x.Item2)
            .Take(5)
            .ToList();

        var products = await _db.Products.ToListAsync();
        var productCategory = products.ToDictionary(p => p.Id, p => p.Category);

        CategoryBreakdown = revenueOrders
            .SelectMany(o => o.Items)
            .Where(i => productCategory.ContainsKey(i.ProductId))
            .GroupBy(i => productCategory[i.ProductId])
            .Select(g => (g.Key.ToString(), g.Sum(i => i.Quantity * i.UnitPrice)))
            .OrderByDescending(x => x.Item2)
            .ToList();

        StatusBreakdown = allOrders
            .GroupBy(o => o.Status)
            .Select(g => (g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }
}