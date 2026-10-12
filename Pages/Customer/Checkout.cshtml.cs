using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer;

public class CheckoutModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IVoucherService _voucherService;
    private readonly IPaymentGateway _paymentGateway;

    public CheckoutModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IVoucherService voucherService,
        IPaymentGateway paymentGateway)
    {
        _db = db;
        _userManager = userManager;
        _voucherService = voucherService;
        _paymentGateway = paymentGateway;
    }

    public List<CartItem> CartItems { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total => Math.Max(0, Subtotal - Discount + Shipping);
    public string? AppliedVoucherCode { get; set; }
    public IReadOnlyList<string> AppliedVoucherCodes { get; set; } = [];
    public string? VoucherMessage { get; set; }
    public IReadOnlyList<VoucherRecommendation> VoucherRecommendations { get; set; } = [];
    public IReadOnlyList<VoucherRecommendation> EligibleVouchers => VoucherRecommendations.Where(v => v.IsEligible).ToList();
    public IReadOnlyList<VoucherRecommendation> EligibleRewardVouchers => EligibleVouchers
        .Where(v => v.Voucher.Group is VoucherGroup.Cashback or VoucherGroup.Discount)
        .ToList();
    public VoucherRecommendation? ShippingVoucher => VoucherRecommendations.FirstOrDefault(v => v.Voucher.Group == VoucherGroup.Shipping);
    public bool ShowOrderConfirmation { get; set; }
    public string? ProfileMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";
        [Required] public string PaymentMethod { get; set; } = "GatewayBanking";
        public string? RewardVoucherCode { get; set; }
        public bool ConfirmOrder { get; set; } = true;
        public string? SelectedCartItemIds { get; set; }
    }

    public decimal GetLineDiscount(CartItem item) => _voucherService.CalculateLineDiscount(item);
    public decimal GetLineTotal(CartItem item) => _voucherService.CalculateLineTotal(item);

    public async Task<IActionResult> OnGetAsync(string? rewardVoucher = null, string? selected = null)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToPage("/Account/Login");

        await LoadCartAsync(user.Id, rewardVoucher, selected);
        if (!CartItems.Any())
            return RedirectToPage("/Customer/ShoppingCart");

        var availability = StockAvailabilityService.Validate(CartItems);
        if (!availability.IsAvailable)
        {
            ModelState.AddModelError(string.Empty, "Some items are out of stock or quantity changed. Please update your cart.");
        }

        if (!HasCompleteCheckoutProfile(user))
        {
            TempData["ProfileMessage"] = "Please complete your name, email, phone number, and default address before checkout. You can leave change password blank.";
            return RedirectToPage("/Customer/Profile");
        }

        Input = new InputModel
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber ?? "",
            Address = user.Address ?? "",
            RewardVoucherCode = AppliedVoucherCodes.FirstOrDefault(code =>
                VoucherRecommendations.Any(v => v.Code == code && v.Voucher.Group is VoucherGroup.Cashback or VoucherGroup.Discount)),
            PaymentMethod = "GatewayBanking",
            SelectedCartItemIds = selected
        };

        return Page();
    }

    public async Task<IActionResult> OnPostPlaceOrderAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToPage("/Account/Login");

        await LoadCartAsync(user.Id, Input.RewardVoucherCode, Input.SelectedCartItemIds);
        if (!CartItems.Any())
            return RedirectToPage("/Customer/ShoppingCart");

        var availability = StockAvailabilityService.Validate(CartItems);
        if (!availability.IsAvailable)
        {
            ModelState.AddModelError(string.Empty, "Some items are out of stock or quantity changed. Please update your cart.");
            Input.FullName = user.FullName;
            Input.Email = user.Email ?? "";
            Input.PhoneNumber = user.PhoneNumber ?? "";
            Input.Address = user.Address ?? "";
            return Page();
        }

        if (!HasCompleteCheckoutProfile(user))
        {
            TempData["ProfileMessage"] = "Please complete your name, email, phone number, and default address before checkout. You can leave change password blank.";
            return RedirectToPage("/Customer/Profile");
        }

        Input.FullName = user.FullName;
        Input.Email = user.Email ?? "";
        Input.PhoneNumber = user.PhoneNumber ?? "";
        Input.Address = user.Address ?? "";
        ClearSavedDeliveryModelState();

        if (!ModelState.IsValid)
            return Page();

        var order = new Order
        {
            CustomerId = user.Id,
            CustomerName = Input.FullName.Trim(),
            CustomerEmail = Input.Email.Trim(),
            DeliveryAddress = Input.Address.Trim(),
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = Input.PaymentMethod,
            OrderDate = DateTime.UtcNow,
            Total = Total,
            VoucherCode = AppliedVoucherCode,
            Discount = Discount,
            ShippingFee = Shipping,
            Items = CartItems.Select(c => new OrderItem
            {
                ProductId = c.ProductId,
                ProductName = c.Product?.Name ?? "Product",
                Quantity = c.Quantity,
                UnitPrice = c.Product?.Price ?? 0m
            }).ToList()
        };

        foreach (var item in CartItems)
        {
            if (item.Product is not null)
            {
                item.Product.Stock = Math.Max(0, item.Product.Stock - item.Quantity);
            }
        }

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(CartItems);
        user.FullName = Input.FullName.Trim();
        user.Address = Input.Address.Trim();
        user.PhoneNumber = Input.PhoneNumber.Trim();
        await _userManager.UpdateAsync(user);
        await _db.SaveChangesAsync();

        var returnUrl = Url.PageLink("/Customer/StripeReturn") ?? $"{Request.Scheme}://{Request.Host}/Customer/StripeReturn";
        var paymentRequest = await _paymentGateway.CreatePaymentRequestAsync(order, returnUrl);
        order.PaymentReference = paymentRequest.Reference;
        await _db.SaveChangesAsync();

        return Redirect(paymentRequest.RedirectUrl);
    }

    private async Task LoadCartAsync(string userId, string? rewardVoucher, string? selected)
    {
        var selectedIds = ParseSelectedIds(selected);

        var query = _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .AsQueryable();

        if (selectedIds.Count > 0)
            query = query.Where(c => selectedIds.Contains(c.Id));

        CartItems = await query.OrderBy(c => c.Id).ToListAsync();
        VoucherRecommendations = _voucherService.GetRecommendedVouchers(CartItems);
        var selectedRewardVoucher = ResolveRewardVoucherCode(rewardVoucher);
        var totals = _voucherService.CalculateTotals(CartItems, selectedRewardVoucher, null);
        Subtotal = totals.Subtotal;
        Discount = totals.Discount;
        Shipping = totals.Shipping;
        AppliedVoucherCode = totals.AppliedVoucherCode;
        AppliedVoucherCodes = totals.AppliedVoucherCodes;
        VoucherMessage = totals.VoucherMessage;
    }

    private string? ResolveRewardVoucherCode(string? rewardVoucher)
    {
        var code = rewardVoucher?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return VoucherRecommendations.Any(v =>
            v.IsEligible &&
            v.Code == code &&
            v.Voucher.Group is VoucherGroup.Cashback or VoucherGroup.Discount)
            ? code
            : null;
    }

    private static List<int> ParseSelectedIds(string? selected)
        => string.IsNullOrWhiteSpace(selected)
            ? []
            : selected.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
    }

    private static bool HasCompleteCheckoutProfile(ApplicationUser user) =>
        !string.IsNullOrWhiteSpace(user.FullName)
        && !string.IsNullOrWhiteSpace(user.Email)
        && !string.IsNullOrWhiteSpace(user.PhoneNumber)
        && !string.IsNullOrWhiteSpace(user.Address);

    private void ClearSavedDeliveryModelState()
    {
        ModelState.Remove("Input.FullName");
        ModelState.Remove("Input.Email");
        ModelState.Remove("Input.PhoneNumber");
        ModelState.Remove("Input.Address");
    }
}
