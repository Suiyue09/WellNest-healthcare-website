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
    public class OrderDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderDetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public Order? Order { get; set; }
        public List<OrderItem> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");
            Order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (Order == null) return NotFound();

            // Unclaimed orders are visible to any rider (shared pool). Once an order is
            // assigned, only that rider (or the Delivery Admin) may view/manage it.
            if (!User.IsDeliveryAdmin() && Order.AssignedDeliveryId != null && Order.AssignedDeliveryId != user.Id)
                return RedirectToPage("/Delivery/AssignOrders");

            Items = await _db.OrderItems
                .Where(oi => oi.OrderId == id)
                .ToListAsync();
            return Page();
        }
    }
}