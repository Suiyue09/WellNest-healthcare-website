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
    [Authorize(Roles = "Delivery")]
    public class AssignOrdersModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICustomerNotificationService _notificationService;

        public AssignOrdersModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ICustomerNotificationService notificationService)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
        }
        [BindProperty(SupportsGet = true)]
        public string Tab { get; set; } = "pickup";
        public List<Order> Orders { get; set; } = new();
        public int ToPickUpCount { get; set; }
        public int OutForDeliveryCount { get; set; }
        public int DeliveredCount { get; set; }
        public int FailedCount { get; set; }

        public string? Message { get; set; }
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            bool isAdmin = User.IsDeliveryAdmin();
            var all = _db.Orders.AsQueryable();

            // The "pickup" pool (Pending/Packed) is a shared board: everyone sees unclaimed
            // orders so they can claim one, plus their own already-claimed pickup-stage orders.
            // Every other stage (out for delivery / delivered / failed) is private to the rider
            // who owns it. The Delivery Admin account always sees everything.
            IQueryable<Order> Scope(IQueryable<Order> q, bool includeUnassigned)
            {
                if (isAdmin) return q;
                return includeUnassigned
                    ? q.Where(o => o.AssignedDeliveryId == null || o.AssignedDeliveryId == user.Id)
                    : q.Where(o => o.AssignedDeliveryId == user.Id);
            }

            ToPickUpCount = await Scope(all.Where(o => o.Status == OrderStatus.Packed || o.Status == OrderStatus.Pending), includeUnassigned: true).CountAsync();
            OutForDeliveryCount = await Scope(all.Where(o => o.Status == OrderStatus.OutForDelivery), includeUnassigned: false).CountAsync();
            DeliveredCount = await Scope(all.Where(o => o.Status == OrderStatus.Delivered), includeUnassigned: false).CountAsync();
            FailedCount = await Scope(all.Where(o => o.Status == OrderStatus.Failed), includeUnassigned: false).CountAsync();

            var query = Tab switch
            {
                "outfordelivery" => Scope(all.Where(o => o.Status == OrderStatus.OutForDelivery), includeUnassigned: false),
                "delivered" => Scope(all.Where(o => o.Status == OrderStatus.Delivered), includeUnassigned: false),
                "failed" => Scope(all.Where(o => o.Status == OrderStatus.Failed), includeUnassigned: false),
                _ => Scope(all.Where(o => o.Status == OrderStatus.Packed || o.Status == OrderStatus.Pending), includeUnassigned: true)
            };
            Orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSendViaCourierAsync(int id, string shippingProvider, string trackingNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return NotFound();

            if (!User.IsDeliveryAdmin() && order.AssignedDeliveryId != null && order.AssignedDeliveryId != user.Id)
                return RedirectToPage(new { tab = Tab });

            if (string.IsNullOrWhiteSpace(shippingProvider) || string.IsNullOrWhiteSpace(trackingNumber))
            {
                Message = "Please enter both a courier name and a tracking number.";
                IsSuccess = false;
                return RedirectToPage(new { tab = Tab });
            }

            order.ShippingProvider = shippingProvider.Trim();
            order.TrackingNumber = trackingNumber.Trim();
            order.Status = OrderStatus.Shipped;

            await _db.SaveChangesAsync();
            await _notificationService.SendOrderStatusChangedAsync(order);

            TempData["CourierMessage"] = $"Order #{order.Id} marked as Shipped via {order.ShippingProvider} — customer notified.";
            return RedirectToPage(new { tab = Tab });
        }
    }
}