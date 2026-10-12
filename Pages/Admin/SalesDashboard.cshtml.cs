using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_Industry.Pages.Admin;

public class SalesDashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public SalesDashboardModel(ApplicationDbContext db) => _db = db;

    // Filter controls
    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "14";

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "qty";

    public string ReportPeriodLabel { get; set; } = "Last 14 days";
    public string TopProductsSortLabel { get; set; } = "Quantity sold";

    // Top stat cards
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AvgOrderValue { get; set; }
    public int CancelledCount { get; set; }
    public decimal PendingCodAmount { get; set; } // COD orders delivered/out but not yet collected

    // Revenue trend for the selected period (for a simple bar chart)
    public List<string> TrendLabels { get; set; } = new();
    public List<decimal> TrendValues { get; set; } = new();

    // Top 5 best-selling products
    public List<(string Name, int Qty, decimal Revenue)> TopProducts { get; set; } = new();

    // Revenue by product category
    public List<(string Category, decimal Revenue)> CategoryBreakdown { get; set; } = new();

    // Order count by status
    public List<(string Status, int Count)> StatusBreakdown { get; set; } = new();

    // An order only counts as real revenue once the money is actually in hand:
    // - COD orders: only after the rider marks CodCollected = true
    // - Non-COD orders: only once payment status is Paid
    private static bool IsRevenueRecognized(Order o)
    {
        if (o.Status == OrderStatus.Cancelled) return false;
        if (o.CodAmount > 0) return o.IsCodCollected;
        return o.PaymentStatus == PaymentStatus.Paid;
    }

    private DateTime? GetStartDate(DateTime today)
    {
        return Period switch
        {
            "today" => today,
            "7" => today.AddDays(-6),
            "30" => today.AddDays(-29),
            "all" => null,
            _ => today.AddDays(-13)
        };
    }

    private string GetPeriodLabel()
    {
        return Period switch
        {
            "today" => "Today",
            "7" => "Last 7 days",
            "30" => "Last 30 days",
            "all" => "All time",
            _ => "Last 14 days"
        };
    }

    private string GetSortLabel()
    {
        return SortBy switch
        {
            "revenue" => "Revenue",
            "name" => "Product name",
            _ => "Quantity sold"
        };
    }

    public async Task OnGetAsync()
    {
        var today = DateTime.UtcNow.Date;
        var startDate = GetStartDate(today);
        ReportPeriodLabel = GetPeriodLabel();
        TopProductsSortLabel = GetSortLabel();

        var allOrdersRaw = await _db.Orders.Include(o => o.Items).ToListAsync();

        // Apply date filter to all sales report calculations.
        var allOrders = allOrdersRaw
            .Where(o => startDate is null || o.OrderDate.Date >= startDate.Value)
            .ToList();

        var validOrders = allOrders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var revenueOrders = allOrders.Where(IsRevenueRecognized).ToList();

        // --- Stat cards ---
        TotalRevenue = revenueOrders.Sum(o => o.Total);
        TotalOrders = allOrders.Count;
        AvgOrderValue = revenueOrders.Count > 0 ? Math.Round(revenueOrders.Average(o => o.Total), 2) : 0;
        CancelledCount = allOrders.Count(o => o.Status == OrderStatus.Cancelled);

        // COD money that's out for delivery / delivered-but-not-yet-marked-collected
        PendingCodAmount = validOrders
            .Where(o => o.CodAmount > 0 && !o.IsCodCollected)
            .Sum(o => o.CodAmount);

        // --- Revenue trend ---
        BuildTrend(revenueOrders, today, startDate);

        // --- Top products ---
        var topProductsQuery = revenueOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductName)
            .Select(g => (Name: g.Key, Qty: g.Sum(i => i.Quantity), Revenue: g.Sum(i => i.Quantity * i.UnitPrice)));

        TopProducts = SortBy switch
        {
            "revenue" => topProductsQuery.OrderByDescending(x => x.Revenue).Take(5).ToList(),
            "name" => topProductsQuery.OrderBy(x => x.Name).Take(5).ToList(),
            _ => topProductsQuery.OrderByDescending(x => x.Qty).Take(5).ToList()
        };

        // --- Revenue by category (join order items back to current product catalogue) ---
        var products = await _db.Products.ToListAsync();
        var productCategory = products.ToDictionary(p => p.Id, p => p.Category);
        CategoryBreakdown = revenueOrders
            .SelectMany(o => o.Items)
            .Where(i => productCategory.ContainsKey(i.ProductId))
            .GroupBy(i => productCategory[i.ProductId])
            .Select(g => (g.Key.ToString(), g.Sum(i => i.Quantity * i.UnitPrice)))
            .OrderByDescending(x => x.Item2)
            .ToList();

        // --- Orders by status ---
        StatusBreakdown = allOrders
            .GroupBy(o => o.Status)
            .Select(g => (g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }

    private void                                                                                                            Trend(List<Order> revenueOrders, DateTime today, DateTime? startDate)
    {
        TrendLabels.Clear();
        TrendValues.Clear();

        // For normal date ranges, show daily bars.
        if (Period != "all")
        {
            var firstDay = startDate ?? today.AddDays(-13);
            var days = (today - firstDay).Days;

            for (int i = 0; i <= days; i++)
            {
                var day = firstDay.AddDays(i);
                var dayTotal = revenueOrders
                    .Where(o => o.OrderDate.Date == day)
                    .Sum(o => o.Total);

                TrendLabels.Add(day.ToString("d MMM"));
                TrendValues.Add(dayTotal);
            }

            return;
        }

        // For all time, use daily bars if the data range is short, otherwise group by month.
        if (!revenueOrders.Any())
        {
            for (int i = 13; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                TrendLabels.Add(day.ToString("d MMM"));
                TrendValues.Add(0m);
            }

            return;
        }

        var minDate = revenueOrders.Min(o => o.OrderDate.Date);
        var dayCount = (today - minDate).Days + 1;

        if (dayCount <= 31)
        {
            for (int i = 0; i < dayCount; i++)
            {
                var day = minDate.AddDays(i);
                var dayTotal = revenueOrders
                    .Where(o => o.OrderDate.Date == day)
                    .Sum(o => o.Total);

                TrendLabels.Add(day.ToString("d MMM"));
                TrendValues.Add(dayTotal);
            }
        }
        else
        {
            var monthlyTotals = revenueOrders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Total = g.Sum(o => o.Total)
                })
                .ToList();

            foreach (var month in monthlyTotals)
            {
                TrendLabels.Add(month.Label);
                TrendValues.Add(month.Total);
            }
        }
    }
}
