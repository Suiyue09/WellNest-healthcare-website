using Healthcare_Industry.Models;

namespace Healthcare_Industry.Services;

public enum VoucherType
{
    Percentage,
    FixedAmount,
    Cashback,
    FreeShipping
}

public enum VoucherGroup
{
    Discount,
    Cashback,
    Shipping
}

public sealed class Voucher
{
    public string Code { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public VoucherType Type { get; init; }
    public VoucherGroup Group { get; init; }
    public decimal Value { get; init; }
    public decimal MinimumSubtotal { get; init; }
    public ProductCategory? Category { get; init; }
}

public sealed class VoucherRecommendation
{
    public required Voucher Voucher { get; init; }
    public required bool IsEligible { get; init; }
    public required decimal EstimatedSavings { get; init; }
    public required string Reason { get; init; }
    public string Code => Voucher.Code;
}

public sealed class CheckoutTotals
{
    public decimal Subtotal { get; init; }
    public decimal ProductPromotionDiscount { get; init; }
    public decimal VoucherDiscount { get; init; }
    public decimal Discount { get; init; }
    public decimal Shipping { get; init; }
    public decimal Total { get; init; }
    public string? AppliedVoucherCode { get; init; }
    public IReadOnlyList<string> AppliedVoucherCodes { get; init; } = [];
    public string? VoucherMessage { get; init; }
}
