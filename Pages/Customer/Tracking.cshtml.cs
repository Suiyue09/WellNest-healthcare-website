using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer;

public class TrackingModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IOrderWorkflowService _orderWorkflow;
    private readonly ICustomerNotificationService _notificationService;

    public TrackingModel(
        ApplicationDbContext db,
        IPaymentGateway paymentGateway,
        IOrderWorkflowService orderWorkflow,
        ICustomerNotificationService notificationService)
    {
        _db = db;
        _paymentGateway = paymentGateway;
        _orderWorkflow = orderWorkflow;
        _notificationService = notificationService;
    }

    public Order? Order { get; set; }
    public Dictionary<int, ProductReview> ReviewsByProductId { get; set; } = new();
    public Dictionary<int, string> ProductImageUrls { get; set; } = new();
    public string CustomerInitial { get; set; } = "U";
    public string CustomerPhoneNumber { get; set; } = "";

    public string? RiderName { get; set; }
    public string? RiderPhone { get; set; }

    [BindProperty]
    public ReviewInput Review { get; set; } = new();

    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
    public IReadOnlyList<string> CancellationReasons { get; } =
    [
        "Changed my mind",
        "Ordered by mistake",
        "Wrong product or quantity",
        "Delivery address is incorrect",
        "Found a better price elsewhere"
    ];

    public class ReviewInput
    {
        [Required] public int ProductId { get; set; }
        [Required, Range(1, 5)] public int Rating { get; set; } = 5;
        [Required, StringLength(600, MinimumLength = 5)] public string Comment { get; set; } = "";
    }

    public IReadOnlyList<OrderStatus> Steps { get; } = new[]
    {
        OrderStatus.Pending,
        OrderStatus.Packed,
        OrderStatus.Shipped,
        OrderStatus.Delivered
    };

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
            return RedirectToPage("/Account/Login");

        Order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

        if (Order is not null)
        {
            CustomerInitial = CustomerDisplayService.GetInitial(Order.CustomerName, Order.CustomerEmail);
            await LoadCustomerPhoneAsync(Order.CustomerId);
            await LoadProductImagesAsync(Order);
            await LoadReviewsAsync(userId, Order.Id);
            await LoadRiderInfoAsync(Order);
        }

        return Order is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostReviewAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
            return RedirectToPage("/Account/Login");

        Order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

        if (Order is null)
            return NotFound();

        CustomerInitial = CustomerDisplayService.GetInitial(Order.CustomerName, Order.CustomerEmail);
        await LoadCustomerPhoneAsync(Order.CustomerId);
        await LoadProductImagesAsync(Order);
        await LoadRiderInfoAsync(Order);

        if (Order.Status != OrderStatus.Delivered)
        {
            Message = "Reviews can be submitted after the order is delivered.";
            IsSuccess = false;
            await LoadReviewsAsync(userId, Order.Id);
            return Page();
        }

        if (!Order.Items.Any(i => i.ProductId == Review.ProductId))
            return BadRequest();

        await LoadReviewsAsync(userId, Order.Id);
        if (ReviewsByProductId.ContainsKey(Review.ProductId))
        {
            Message = "You have already reviewed this product from this order.";
            IsSuccess = false;
            return Page();
        }

        if (!ModelState.IsValid)
        {
            Message = "Choose a rating and write at least 5 characters for your review.";
            IsSuccess = false;
            return Page();
        }

        _db.ProductReviews.Add(new ProductReview
        {
            ProductId = Review.ProductId,
            OrderId = Order.Id,
            CustomerId = userId,
            CustomerName = Order.CustomerName,
            Rating = Review.Rating,
            Comment = Review.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        Message = "Thank you. Your review is now shown on the product page.";
        IsSuccess = true;
        Review = new ReviewInput();
        await LoadReviewsAsync(userId, Order.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostRetryPaymentAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
            return RedirectToPage("/Account/Login");

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

        if (order is null)
            return NotFound();

        if (order.PaymentStatus == PaymentStatus.Paid || order.Status == OrderStatus.Cancelled)
            return RedirectToPage("/Customer/Tracking", new { id = order.Id });

        order.PaymentStatus = PaymentStatus.Pending;
        var returnUrl = Url.PageLink("/Customer/StripeReturn") ?? $"{Request.Scheme}://{Request.Host}/Customer/StripeReturn";
        var paymentRequest = await _paymentGateway.CreatePaymentRequestAsync(order, returnUrl);
        order.PaymentReference = paymentRequest.Reference;
        await _db.SaveChangesAsync();

        return Redirect(paymentRequest.RedirectUrl);
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string cancellationReason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
            return RedirectToPage("/Account/Login");

        Order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

        if (Order is null)
            return NotFound();

        CustomerInitial = CustomerDisplayService.GetInitial(Order.CustomerName, Order.CustomerEmail);
        await LoadCustomerPhoneAsync(Order.CustomerId);
        await LoadProductImagesAsync(Order);
        await LoadRiderInfoAsync(Order);

        if (!_orderWorkflow.CanCustomerCancel(Order))
        {
            Message = "This order can no longer be cancelled.";
            IsSuccess = false;
            await LoadReviewsAsync(userId, Order.Id);
            return Page();
        }

        if (string.IsNullOrWhiteSpace(cancellationReason) || !CancellationReasons.Contains(cancellationReason))
        {
            Message = "Choose a cancellation reason.";
            IsSuccess = false;
            await LoadReviewsAsync(userId, Order.Id);
            return Page();
        }

        Order.Status = OrderStatus.Cancelled;
        Order.CancellationReason = cancellationReason;
        Order.CancelledAt = DateTime.UtcNow;
        if (Order.PaymentStatus == PaymentStatus.Paid)
        {
            Order.PaymentStatus = PaymentStatus.Refunded;
        }

        await _db.SaveChangesAsync();
        await _notificationService.SendOrderStatusChangedAsync(Order);

        Message = "Order cancelled successfully.";
        IsSuccess = true;
        await LoadReviewsAsync(userId, Order.Id);
        return Page();
    }

    private async Task LoadReviewsAsync(string userId, int orderId)
    {
        ReviewsByProductId = await _db.ProductReviews
            .Where(r => r.CustomerId == userId && r.OrderId == orderId)
            .ToDictionaryAsync(r => r.ProductId);
    }

    private async Task LoadProductImagesAsync(Order order)
    {
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        ProductImageUrls = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.ImageUrl != null)
            .ToDictionaryAsync(p => p.Id, p => p.ImageUrl!);
    }

    private async Task LoadCustomerPhoneAsync(string customerId)
    {
        CustomerPhoneNumber = await _db.Users
            .Where(u => u.Id == customerId)
            .Select(u => u.PhoneNumber ?? "")
            .FirstOrDefaultAsync() ?? "";
    }

    private async Task LoadRiderInfoAsync(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.AssignedDeliveryId))
            return;

        var rider = await _db.Users
            .Where(u => u.Id == order.AssignedDeliveryId)
            .Select(u => new { u.FullName, u.PhoneNumber })
            .FirstOrDefaultAsync();

        if (rider is not null)
        {
            RiderName = rider.FullName;
            RiderPhone = rider.PhoneNumber;
        }
    }
}
