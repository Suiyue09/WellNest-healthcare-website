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
    public class UpdateStatusModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICustomerNotificationService _notificationService;
        private readonly IWebHostEnvironment _env;

        public UpdateStatusModel(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ICustomerNotificationService notificationService,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
            _env = env;
        }

        public Order? Order { get; set; }
        [BindProperty] public bool CodCollected { get; set; }
        [BindProperty] public string? FailedReason { get; set; }
        [BindProperty] public string? ProofNote { get; set; }
        [BindProperty] public IFormFile? ProofImage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");
            Order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (Order == null) return NotFound();
            if (!User.IsDeliveryAdmin() && Order.AssignedDeliveryId != null && Order.AssignedDeliveryId != user.Id)
                return RedirectToPage("/Delivery/AssignOrders");
            CodCollected = Order.IsCodCollected;
            return Page();
        }

        public async Task<IActionResult> OnPostClaimAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            if (string.IsNullOrEmpty(order.AssignedDeliveryId))
            {
                order.AssignedDeliveryId = user.Id;
                order.Status = OrderStatus.Packed;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("/Delivery/OrderDetails", new { id });
        }

        public async Task<IActionResult> OnPostAdvanceAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            if (!User.IsDeliveryAdmin() && order.AssignedDeliveryId != null && order.AssignedDeliveryId != user.Id)
                return RedirectToPage("/Delivery/AssignOrders");

            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Packed;
            }
            else if (order.Status == OrderStatus.Packed)
            {
                order.Status = OrderStatus.OutForDelivery;
                if (string.IsNullOrEmpty(order.AssignedDeliveryId))
                    order.AssignedDeliveryId = user.Id;
            }
            else if (order.Status == OrderStatus.OutForDelivery)
            {
                order.Status = OrderStatus.Delivered;
                order.DeliveredAt = DateTime.UtcNow;
                order.IsCodCollected = CodCollected;
                order.DeliveryNote = ProofNote;

                // Calculate earnings — RM5 base + 20% bonus every 5th delivery
                var completedCount = await _db.Orders
                    .CountAsync(o => o.AssignedDeliveryId == user.Id &&
                                o.Status == OrderStatus.Delivered);

                decimal baseEarning = 5.00m;
                bool isBonusDelivery = (completedCount + 1) % 5 == 0;
                order.DeliveryEarning = isBonusDelivery
                    ? baseEarning * 1.20m
                    : baseEarning;

                // Save proof image
                if (ProofImage != null && ProofImage.Length > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "delivery-proofs");
                    Directory.CreateDirectory(uploadsDir);
                    var ext = Path.GetExtension(ProofImage.FileName);
                    var fileName = $"proof-{id}{ext}";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await ProofImage.CopyToAsync(stream);
                    order.DeliveryProofImageUrl = $"/uploads/delivery-proofs/{fileName}";
                }
            }

            await _db.SaveChangesAsync();
            await _notificationService.SendOrderStatusChangedAsync(order);
            return RedirectToPage("/Delivery/OrderDetails", new { id = order.Id });
        }

        public async Task<IActionResult> OnPostFailAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            if (!User.IsDeliveryAdmin() && order.AssignedDeliveryId != null && order.AssignedDeliveryId != user.Id)
                return RedirectToPage("/Delivery/AssignOrders");
            order.Status = OrderStatus.Failed;
            order.DeliveryNote = FailedReason;
            await _db.SaveChangesAsync();
            await _notificationService.SendOrderStatusChangedAsync(order);
            return RedirectToPage("/Delivery/AssignOrders");
        }
    }
}