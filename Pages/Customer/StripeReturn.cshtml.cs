using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer;

public class StripeReturnModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICustomerNotificationService _notificationService;
    private readonly IShippingService _shippingService;

    public StripeReturnModel(
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

    public async Task<IActionResult> OnGetAsync(int orderId, string sessionId, string result)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
            return RedirectToPage("/Account/Login");

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

        if (order is null)
            return NotFound();

        if (string.Equals(result, "cancel", StringComparison.OrdinalIgnoreCase))
        {
            order.PaymentStatus = PaymentStatus.Failed;
            order.DeliveryNote = "Stripe Checkout was cancelled before payment.";
            await _db.SaveChangesAsync();
            return RedirectToPage("/Customer/Tracking", new { id = order.Id });
        }

        var verification = await _paymentGateway.VerifyPaymentAsync(order, sessionId, true);
        if (verification.IsSuccessful)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaymentReference = sessionId;
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

