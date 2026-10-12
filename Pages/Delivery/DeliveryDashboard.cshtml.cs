using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Healthcare_Industry.Pages.Delivery
{
    // Dashboard shows system-wide stats and every rider's orders, so it's Delivery Admin only.
    // Riders are bounced to their own "My Orders" (AssignOrders) page instead.
    [Authorize(Roles = "Delivery")]
    public class DeliveryDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        public DeliveryDashboardModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }
        public int ToPickUpCount { get; set; }
        public int OutForDeliveryCount { get; set; }
        public int DeliveredTodayCount { get; set; }
        public decimal CodToCollect { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public Dictionary<string, string> RiderNames { get; set; } = new();
        public int TotalFilteredCount { get; set; }

        // Earnings
        public decimal TotalEarned { get; set; }
        public int TotalDelivered { get; set; }
        public int CurrentStreak { get; set; }
        public int DeliveriesUntilBonus { get; set; }
        public decimal NextBonusAmount { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? AssignedFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");
            if (!User.IsDeliveryAdmin()) return RedirectToPage("/Delivery/AssignOrders");
            var today = DateTime.UtcNow.Date;

            ToPickUpCount = await _db.Orders
                .CountAsync(o => o.Status == OrderStatus.Packed ||
                            o.Status == OrderStatus.Pending);
            OutForDeliveryCount = await _db.Orders
                .CountAsync(o => o.Status == OrderStatus.OutForDelivery);
            DeliveredTodayCount = await _db.Orders
    .CountAsync(o => o.Status == OrderStatus.Delivered);


            // Build filtered query
            var query = _db.Orders.AsQueryable();

            // Filter 1: Search by customer name or order ID
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var searchLower = Search.Trim().ToLower();
                if (int.TryParse(Search.Trim(), out int orderId))
                    query = query.Where(o => o.Id == orderId);
                else
                    query = query.Where(o => o.CustomerName.ToLower().Contains(searchLower));
            }

            // Filter 2: Order status
            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
                Enum.TryParse<OrderStatus>(StatusFilter, out var parsedStatus))
                query = query.Where(o => o.Status == parsedStatus);

            // Filter 3: Rider assigned / not assigned
            if (AssignedFilter == "assigned")
                query = query.Where(o => o.AssignedDeliveryId != null);
            else if (AssignedFilter == "unassigned")
                query = query.Where(o => o.AssignedDeliveryId == null);

            // Filter 4: Date range
            if (FromDate.HasValue)
                query = query.Where(o => o.OrderDate.Date >= FromDate.Value.Date);
            if (ToDate.HasValue)
                query = query.Where(o => o.OrderDate.Date <= ToDate.Value.Date);

            TotalFilteredCount = await query.CountAsync();

            RecentOrders = await query
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .ToListAsync();

            // Look up rider names for anyone shown in this table
            var riderIdsOnPage = RecentOrders
                .Where(o => o.AssignedDeliveryId != null)
                .Select(o => o.AssignedDeliveryId!)
                .Distinct()
                .ToList();

            if (riderIdsOnPage.Any())
            {
                var riderUsers = await _db.Users
                    .Where(u => riderIdsOnPage.Contains(u.Id))
                    .ToListAsync();

                RiderNames = riderUsers.ToDictionary(
                    u => u.Id,
                    u => string.IsNullOrWhiteSpace(u.FullName) ? (u.Email ?? "Unknown rider") : u.FullName);
            }

            // Earnings calculations
            var myDelivered = await _db.Orders
                .Where(o => o.AssignedDeliveryId == user.Id &&
                            o.Status == OrderStatus.Delivered)
                .ToListAsync();
            TotalDelivered = myDelivered.Count;
            TotalEarned = myDelivered.Sum(o => o.DeliveryEarning);
            CurrentStreak = TotalDelivered % 5;
            DeliveriesUntilBonus = CurrentStreak == 0 ? 5 : 5 - CurrentStreak;
            NextBonusAmount = 5m * 0.20m;
            return Page();
        }
    }
}