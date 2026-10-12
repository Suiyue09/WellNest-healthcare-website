using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer;

public class ShoppingCartModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IVoucherService _voucherService;

    public ShoppingCartModel(ApplicationDbContext db, IVoucherService voucherService)
    {
        _db = db;
        _voucherService = voucherService;
    }

    public List<CartItem> CartItems { get; set; } = new();

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total => Math.Max(0, Subtotal - Discount + Shipping);
    public string? Message { get; set; }
    public string CustomerInitial { get; set; } = "U";

    public decimal GetLineDiscount(CartItem item) => _voucherService.CalculateLineDiscount(item);
    public decimal GetLineTotal(CartItem item) => _voucherService.CalculateLineTotal(item);

    public async Task<IActionResult> OnGetAsync()
    {
        if (!TryGetUserId(out var userId))
            return RedirectToPage("/Account/Login");

        await LoadCartAsync(userId);
        await LoadCustomerInitialAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateQtyAsync(int cartItemId, [Range(1, 99)] int quantity)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToPage("/Account/Login");

        var item = await _db.CartItems.Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
        if (item is not null && item.Product is not null)
        {
            item.Quantity = Math.Clamp(quantity, 1, Math.Max(1, item.Product.Stock));
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Customer/ShoppingCart");
    }

    public async Task<IActionResult> OnPostRemoveAsync(int cartItemId)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToPage("/Account/Login");

        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
        if (item is not null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Customer/ShoppingCart");
    }

    public async Task<IActionResult> OnPostRemoveSelectedAsync(List<int> selectedItemIds)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToPage("/Account/Login");

        if (selectedItemIds.Count > 0)
        {
            var items = await _db.CartItems
                .Where(c => c.UserId == userId && selectedItemIds.Contains(c.Id))
                .ToListAsync();

            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Customer/ShoppingCart");
    }

    public async Task<IActionResult> OnPostCheckoutSelectedAsync(List<int> selectedItemIds)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToPage("/Account/Login");

        await LoadCartAsync(userId);
        if (selectedItemIds.Count == 0)
        {
            await LoadCustomerInitialAsync(userId);
            Message = "Select at least one item to checkout.";
            return Page();
        }

        var selectedItems = CartItems
            .Where(c => selectedItemIds.Contains(c.Id))
            .ToList();

        var availability = StockAvailabilityService.Validate(selectedItems);
        if (!availability.IsAvailable)
        {
            await LoadCustomerInitialAsync(userId);
            Message = "Some selected items are out of stock or quantity changed.";
            return Page();
        }

        var validIds = selectedItems
            .Select(c => c.Id)
            .ToList();

        if (validIds.Count == 0)
        {
            await LoadCustomerInitialAsync(userId);
            Message = "The selected items are no longer available in your cart.";
            return Page();
        }

        return RedirectToPage("/Customer/Checkout", new
        {
            selected = string.Join(",", validIds)
        });
    }

    private async Task LoadCartAsync(string userId)
    {
        CartItems = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Id)
            .ToListAsync();

        var totals = _voucherService.CalculateTotals(CartItems, null);
        Subtotal = totals.Subtotal;
        Discount = totals.Discount;
        Shipping = totals.Shipping;
    }

    private async Task LoadCustomerInitialAsync(string userId)
    {
        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.FullName, u.Email })
            .FirstOrDefaultAsync();

        CustomerInitial = CustomerDisplayService.GetInitial(user?.FullName, user?.Email);
    }

    private bool TryGetUserId(out string userId)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        return User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(userId);
    }
}
