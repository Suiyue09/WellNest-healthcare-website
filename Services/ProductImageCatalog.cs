namespace Healthcare_Industry.Services;

public static class ProductImageCatalog
{
    private static readonly Dictionary<string, string> ImageUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Vitamin C 1000mg"] = "/images/products/vitamin-c.png",
        ["Omega-3 Fish Oil"] = "/images/products/fish-oil.png",
        ["Collagen Peptides"] = "/images/products/collagen-peptides.png",
        ["Magnesium Glycinate"] = "/images/products/magnesium-glycinate.png",
        ["Zinc Immune Support"] = "/images/products/zinc-immune-support.png",
        ["Daily Probiotic Capsules"] = "/images/products/daily-probiotic-capsules.png",
        ["Multivitamin Gummies"] = "/images/products/multivitamin-gummies.png",
        ["Deep Moisture Body Wash"] = "/images/products/body-wash.png",
        ["Lavender Bath Salts"] = "/images/products/lavender-bath-salts.png",
        ["Aloe Vera Body Lotion"] = "/images/products/aloe-vera-body-lotion.png",
        ["Tea Tree Hand Wash"] = "/images/products/tea-tree-hand-wash.png",
        ["Antibacterial Hand Sanitiser"] = "/images/products/antibacterial-hand-sanitiser.png",
        ["Moisturising Cream"] = "/images/products/moisturising-cream.png",
        ["Niacinamide 10% Serum"] = "/images/products/niacinamide-serum.png",
        ["SPF 50 Sunscreen"] = "/images/products/spf-50-sunscreen.png",
        ["Gentle Facial Cleanser"] = "/images/products/gentle-facial-cleanser.png",
        ["Hyaluronic Acid Serum"] = "/images/products/hyaluronic-acid-serum.png",
        ["Retinol Night Cream"] = "/images/products/retinol-night-cream.png",
        ["Hydra Baby Lotion"] = "/images/products/baby-lotion.png",
        ["Baby Dry Pants M"] = "/images/products/baby-dry-pants.png",
        ["Baby Gentle Shampoo"] = "/images/products/baby-gentle-shampoo.png",
        ["Sensitive Baby Wipes"] = "/images/products/sensitive-baby-wipes.png",
        ["Nappy Rash Cream"] = "/images/products/nappy-rash-cream.png",
        ["Blood Pressure Monitor"] = "/images/products/blood-pressure-monitor.png",
        ["Digital Thermometer"] = "/images/products/digital-thermometer.png",
        ["Fingertip Pulse Oximeter"] = "/images/products/pulse-oximeter.png",
        ["Infrared Forehead Thermometer"] = "/images/products/infrared-forehead-thermometer.png",
        ["Glucose Monitoring Kit"] = "/images/products/glucose-monitoring-kit.png",
        ["Portable Nebulizer Machine"] = "/images/products/portable-nebulizer-machine.png"
    };

    public static string? GetImageUrl(string productName)
        => ImageUrls.TryGetValue(productName, out var imageUrl) ? imageUrl : null;
}
