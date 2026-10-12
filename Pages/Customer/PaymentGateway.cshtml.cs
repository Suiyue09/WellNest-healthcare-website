using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer;

public class PaymentGatewayModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICustomerNotificationService _notificationService;
    private readonly IShippingService _shippingService;

    public PaymentGatewayModel(
        ApplicationDbContext db,
        IPaymentGateway paymentGateway,
        ICustomerNotificationService notificationService,
        IShippingService shippingService)
    {
        _db = db;
        _paymentGateway = paymentGateway;
        _notificationService = notificationService;
        _shippingService = shippingService;
    }

    public Order? Order { get; set; }
    public string Reference { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int orderId, string reference)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        Order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

        if (Order is null)
        {
            return NotFound();
        }

        Reference = reference;
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int orderId, string reference, bool success)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

        if (order is null)
        {
            return NotFound();
        }

        var verification = await _paymentGateway.VerifyPaymentAsync(order, reference, success);
        if (verification.IsSuccessful)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaymentTransactionId = verification.ProviderTransactionId;
            order.PaidAt = DateTime.UtcNow;
            order.ShippingProvider = "WellNest Cloud Courier";
            order.TrackingNumber ??= await _shippingService.CreateShipmentAsync(order);
            await _notificationService.SendOrderPaidAsync(order);
        }
        else
        {
            order.PaymentStatus = PaymentStatus.Failed;
            order.DeliveryNote = verification.Message;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/Customer/Tracking", new { id = order.Id });
    }
}
