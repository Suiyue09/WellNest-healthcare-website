namespace Healthcare_Industry.Models;
public class Order
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Total { get; set; }
    public string DeliveryAddress { get; set; } = "";
    public string? AssignedDeliveryId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string PaymentMethod { get; set; } = "";
    public string? PaymentReference { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? DemoCardholderName { get; set; }
    public string? DemoCardLastFour { get; set; }
    public string? DemoCardExpiry { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? VoucherCode { get; set; }
    public decimal Discount { get; set; }
    public decimal ShippingFee { get; set; }
    public string? ShippingProvider { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Delivery fields
    public decimal CodAmount { get; set; } = 0;
    public bool IsCodCollected { get; set; } = false;
    public string? DeliveryNote { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public string? DeliveryProofImageUrl { get; set; }
    public DateTime? ClaimDeadline { get; set; }

    public decimal DeliveryEarning { get; set; } = 0;


}
