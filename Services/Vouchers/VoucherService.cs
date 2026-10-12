using Healthcare_Industry.Models;

namespace Healthcare_Industry.Services;

public interface IVoucherService
{
    IReadOnlyList<VoucherRecommendation> GetRecommendedVouchers(IEnumerable<CartItem> cartItems);
    CheckoutTotals CalculateTotals(IEnumerable<CartItem> cartItems, string? voucherCode);
    CheckoutTotals CalculateTotals(IEnumerable<CartItem> cartItems, string? rewardVoucherCode, string? shippingVoucherCode);
    decimal CalculateLineDiscount(CartItem item);
    decimal CalculateLineTotal(CartItem item);
}

public sealed class VoucherService : IVoucherService
{
    private const decimal StandardShipping = 8m;

    private static readonly IReadOnlyList<Voucher> Vouchers =
    [
        new()
        {
            Code = "WELLNESS10",
            Title = "10% Wellness Saver",
            Description = "10% off orders from RM80.",
            Type = VoucherType.Percentage,
            Group = VoucherGroup.Discount,
            Value = 10m,
            MinimumSubtotal = 80m
        },
        new()
        {
            Code = "CASHBACK5",
            Title = "5% Cashback",
            Description = "Earn 5% cashback on orders from RM60.",
            Type = VoucherType.Cashback,
            Group = VoucherGroup.Cashback,
            Value = 5m,
            MinimumSubtotal = 60m
        },
        new()
        {
            Code = "SUPP15",
            Title = "Supplement Boost",
            Description = "15% off supplement orders from RM100.",
            Type = VoucherType.Percentage,
            Group = VoucherGroup.Discount,
            Value = 15m,
            MinimumSubtotal = 100m,
            Category = ProductCategory.Supplements
        },
        new()
        {
            Code = "BABY20",
            Title = "Baby Care RM20 Off",
            Description = "RM20 off baby care orders from RM150.",
            Type = VoucherType.FixedAmount,
            Group = VoucherGroup.Discount,
            Value = 20m,
            MinimumSubtotal = 150m,
            Category = ProductCategory.BabyCare
        },
        new()
        {
            Code = "FREESHIP120",
            Title = "Free Shipping",
            Description = "Automatically applied when selected items reach RM120.",
            Type = VoucherType.FreeShipping,
            Group = VoucherGroup.Shipping,
            Value = StandardShipping,
            MinimumSubtotal = 120m
        }
    ];

    public IReadOnlyList<VoucherRecommendation> GetRecommendedVouchers(IEnumerable<CartItem> cartItems)
    {
        var items = cartItems.ToList();
        var subtotal = CalculateSubtotal(items);
        var shipping = CalculateBaseShipping(subtotal);

        return Vouchers
            .Select(voucher =>
            {
                var categoryMatches = voucher.Category is null || items.Any(i => i.Product?.Category == voucher.Category);
                var subtotalMatches = subtotal >= voucher.MinimumSubtotal;
                var isEligible = categoryMatches && subtotalMatches;
                var savings = isEligible ? CalculateSavings(voucher, subtotal, shipping) : 0m;
                var reason = BuildReason(voucher, subtotal, categoryMatches, subtotalMatches);

                return new VoucherRecommendation
                {
                    Voucher = voucher,
                    IsEligible = isEligible,
                    EstimatedSavings = savings,
                    Reason = reason
                };
            })
            .OrderByDescending(r => r.IsEligible)
            .ThenByDescending(r => r.EstimatedSavings)
            .ThenBy(r => r.Voucher.MinimumSubtotal)
            .ToList();
    }

    public CheckoutTotals CalculateTotals(IEnumerable<CartItem> cartItems, string? voucherCode)
        => CalculateTotals(cartItems, voucherCode, null);

    public CheckoutTotals CalculateTotals(IEnumerable<CartItem> cartItems, string? rewardVoucherCode, string? shippingVoucherCode)
    {
        var items = cartItems.ToList();
        var subtotal = CalculateSubtotal(items);
        var productPromotionDiscount = items.Sum(CalculateLineDiscount);
        var shipping = CalculateBaseShipping(subtotal);
        var requestedCodes = new[]
            {
                rewardVoucherCode?.Trim().ToUpperInvariant(),
                shippingVoucherCode?.Trim().ToUpperInvariant()
            }
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (subtotal >= 120m && !requestedCodes.Contains("FREESHIP120", StringComparer.OrdinalIgnoreCase))
        {
            requestedCodes.Add("FREESHIP120");
        }

        if (requestedCodes.Count == 0)
        {
            return BuildTotals(subtotal, productPromotionDiscount, 0m, shipping, [], null);
        }

        var recommendations = GetRecommendedVouchers(items);
        var applied = new List<VoucherRecommendation>();
        var messages = new List<string>();

        foreach (var code in requestedCodes.Take(2))
        {
            var recommendation = recommendations.FirstOrDefault(v => string.Equals(v.Code, code, StringComparison.OrdinalIgnoreCase));
            if (recommendation is null)
            {
                messages.Add($"{code} was not recognised.");
                continue;
            }

            if (!recommendation.IsEligible)
            {
                messages.Add(recommendation.Reason);
                continue;
            }

            var hasSameGroup = applied.Any(v => v.Voucher.Group == recommendation.Voucher.Group);
            var hasReward = applied.Any(v => v.Voucher.Group is VoucherGroup.Cashback or VoucherGroup.Discount);
            var isReward = recommendation.Voucher.Group is VoucherGroup.Cashback or VoucherGroup.Discount;

            if (hasSameGroup || (isReward && hasReward))
            {
                messages.Add("Choose only one cashback or discount voucher plus one shipping voucher.");
                continue;
            }

            applied.Add(recommendation);
        }

        var voucherDiscount = applied
            .Where(v => v.Voucher.Type is not VoucherType.FreeShipping)
            .Sum(v => v.EstimatedSavings);
        var finalShipping = applied.Any(v => v.Voucher.Type == VoucherType.FreeShipping) ? 0m : shipping;

        if (applied.Count > 0)
        {
            messages.Insert(0, $"{string.Join(" and ", applied.Select(v => v.Voucher.Title))} applied.");
        }

        return BuildTotals(subtotal, productPromotionDiscount, voucherDiscount, finalShipping, applied.Select(v => v.Code).ToList(), string.Join(" ", messages));
    }

    public decimal CalculateLineDiscount(CartItem item)
    {
        if (item.Product?.Label != ProductLabel.Buy1Free1)
        {
            return 0m;
        }

        var freeUnits = item.Quantity / 2;
        return Math.Round(freeUnits * item.Product.Price, 2);
    }

    public decimal CalculateLineTotal(CartItem item)
    {
        var subtotal = (item.Product?.Price ?? 0m) * item.Quantity;
        return Math.Max(0m, Math.Round(subtotal - CalculateLineDiscount(item), 2));
    }

    private static CheckoutTotals BuildTotals(decimal subtotal, decimal productPromotionDiscount, decimal voucherDiscount, decimal shipping, IReadOnlyList<string> voucherCodes, string? message)
    {
        var totalDiscount = productPromotionDiscount + voucherDiscount;
        return new CheckoutTotals
        {
            Subtotal = subtotal,
            ProductPromotionDiscount = Math.Round(productPromotionDiscount, 2),
            VoucherDiscount = Math.Round(voucherDiscount, 2),
            Discount = Math.Round(totalDiscount, 2),
            Shipping = Math.Round(shipping, 2),
            Total = Math.Max(0m, Math.Round(subtotal - totalDiscount + shipping, 2)),
            AppliedVoucherCode = voucherCodes.Count == 0 ? null : string.Join(",", voucherCodes),
            AppliedVoucherCodes = voucherCodes,
            VoucherMessage = string.IsNullOrWhiteSpace(message) ? null : message
        };
    }

    private static decimal CalculateSubtotal(IEnumerable<CartItem> items)
        => items.Sum(item => (item.Product?.Price ?? 0m) * item.Quantity);

    private static decimal CalculateBaseShipping(decimal subtotal)
        => subtotal == 0m || subtotal >= 120m ? 0m : StandardShipping;

    private static decimal CalculateSavings(Voucher voucher, decimal subtotal, decimal shipping)
        => voucher.Type switch
        {
            VoucherType.Percentage => Math.Round(subtotal * (voucher.Value / 100m), 2),
            VoucherType.FixedAmount => Math.Min(voucher.Value, subtotal),
            VoucherType.Cashback => Math.Round(subtotal * (voucher.Value / 100m), 2),
            VoucherType.FreeShipping => shipping,
            _ => 0m
        };

    private static string BuildReason(Voucher voucher, decimal subtotal, bool categoryMatches, bool subtotalMatches)
    {
        if (!categoryMatches && voucher.Category is not null)
        {
            return $"Add a {voucher.Category} product to use this voucher.";
        }

        if (!subtotalMatches)
        {
            var remaining = voucher.MinimumSubtotal - subtotal;
            return $"Add RM {remaining:0.00} more to unlock this voucher.";
        }

        return "Recommended for your cart.";
    }
}
