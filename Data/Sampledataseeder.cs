using Healthcare_Industry.Models;
using Healthcare_Industry.Pages.Delivery;
using Healthcare_Industry.Services;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_Industry.Data;

// Seeds demo products + orders so the admin Order and Sales pages have data to show.
// Safe to leave in: it only adds data when the tables are (near) empty.
public static class SampleDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        // 1) Seed products by name only when missing (avoids duplicates on later runs)
        var sampleProducts = GetSampleProducts();
        var existingProductNames = await db.Products
            .Select(p => p.Name)
            .ToListAsync();
        var existingNameSet = existingProductNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingProducts = sampleProducts
            .Where(p => !existingNameSet.Contains(p.Name))
            .ToList();

        if (missingProducts.Count > 0)
        {
            db.Products.AddRange(missingProducts);
            await db.SaveChangesAsync();
        }

        await ApplySampleProductImagesAsync(db);

        // 2) Seed sample orders only if there are none yet
        if (await db.Orders.AnyAsync()) return;

        var catalogue = await db.Products.ToListAsync();
        if (catalogue.Count == 0) return; // nothing to build orders from

        var customers = new (string Name, string Email)[]
        {
            ("Sarah Tan",   "sarah.tan@example.com"),
            ("Ahmad Rahman","ahmad.r@example.com"),
            ("Mei Ling Wong","meiling@example.com"),
            ("David Lim",   "david.lim@example.com"),
            ("Nurul Ain",   "nurul.ain@example.com"),
            ("James Koh",   "james.koh@example.com"),
        };

        var statuses = new[]
        {
            OrderStatus.Delivered, OrderStatus.Delivered, OrderStatus.Delivered,
            OrderStatus.Shipped, OrderStatus.Packed, OrderStatus.Pending, OrderStatus.Cancelled,
        };

        var rnd = new Random(42);   // fixed seed = same demo data every fresh seed
        var orders = new List<Order>();

        for (int i = 0; i < 12; i++)
        {
            var cust = customers[rnd.Next(customers.Length)];

            var order = new Order
            {
                CustomerId = Guid.NewGuid().ToString(),
                CustomerName = cust.Name,
                CustomerEmail = cust.Email,
                OrderDate = DateTime.UtcNow.AddDays(-rnd.Next(0, 40)).AddHours(-rnd.Next(0, 24)),
                Status = statuses[rnd.Next(statuses.Length)],
                DeliveryAddress = $"{rnd.Next(1, 200)} Jalan Wellness, Kuala Lumpur",
            };

            // 1–3 distinct products per order
            var lineCount = rnd.Next(1, 4);
            var picks = catalogue.OrderBy(_ => rnd.Next()).Take(lineCount).ToList();

            decimal total = 0m;
            foreach (var p in picks)
            {
                var qty = rnd.Next(1, 4);
                order.Items.Add(new OrderItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Quantity = qty,
                    UnitPrice = p.Price,
                });
                total += qty * p.Price;
            }

            order.Total = total;
            orders.Add(order);
        }

        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
    }

    private static Product WithImage(Product product)
    {
        product.ImageUrl = ProductImageCatalog.GetImageUrl(product.Name);
        return product;
    }

    private static List<Product> GetSampleProducts() =>
    [
        WithImage(new() { Name = "Vitamin C 1000mg", Brand = "WellNest Labs", Category = ProductCategory.Supplements, Price = 45m, Stock = 48, Label = ProductLabel.Bestseller, Description = "High-potency Vitamin C with bioflavonoids." }),
        WithImage(new() { Name = "Omega-3 Fish Oil", Brand = "Nordic Pure", Category = ProductCategory.Supplements, Price = 68m, Stock = 32, Label = ProductLabel.NewArrival, Description = "Ultra-purified EPA & DHA omega-3 softgels." }),
        WithImage(new() { Name = "Collagen Peptides", Brand = "GlowVital", Category = ProductCategory.Supplements, Price = 89m, OldPrice = 99m, Stock = 21, Label = ProductLabel.Bestseller, Description = "Hydrolysed marine collagen for skin and hair." }),
        WithImage(new() { Name = "Magnesium Glycinate", Brand = "CalmCore", Category = ProductCategory.Supplements, Price = 38m, OldPrice = 55m, Stock = 55, Label = ProductLabel.ThirtyOff, Description = "Highly absorbable magnesium for sleep and recovery." }),
        WithImage(new() { Name = "Zinc Immune Support", Brand = "WellNest Labs", Category = ProductCategory.Supplements, Price = 32m, Stock = 40, Label = ProductLabel.NewArrival, Description = "Daily zinc tablets to support immune health." }),
        WithImage(new() { Name = "Daily Probiotic Capsules", Brand = "GutBloom", Category = ProductCategory.Supplements, Price = 72m, OldPrice = 86m, Stock = 24, Label = ProductLabel.OnSale, Description = "Multi-strain probiotics for digestive balance." }),
        WithImage(new() { Name = "Multivitamin Gummies", Brand = "VitaJoy", Category = ProductCategory.Supplements, Price = 48m, Stock = 36, Label = ProductLabel.Bestseller, Description = "Fruit-flavoured daily multivitamin gummies." }),

        WithImage(new() { Name = "Deep Moisture Body Wash", Brand = "Dove", Category = ProductCategory.BathAndBody, Price = 18m, OldPrice = 24m, Stock = 80, Label = ProductLabel.OnSale, Description = "Nourishing body wash with moisturising cream." }),
        WithImage(new() { Name = "Lavender Bath Salts", Brand = "Pure Calm", Category = ProductCategory.BathAndBody, Price = 35m, Stock = 25, Label = ProductLabel.NewArrival, Description = "Epsom and Himalayan salts with lavender oil." }),
        WithImage(new() { Name = "Aloe Vera Body Lotion", Brand = "DermaSoft", Category = ProductCategory.BathAndBody, Price = 28m, Stock = 46, Label = ProductLabel.None, Description = "Lightweight body lotion with soothing aloe vera." }),
        WithImage(new() { Name = "Tea Tree Hand Wash", Brand = "Pure Calm", Category = ProductCategory.BathAndBody, Price = 16m, Stock = 70, Label = ProductLabel.Buy1Free1, Description = "Refreshing hand wash with tea tree extract." }),
        WithImage(new() { Name = "Antibacterial Hand Sanitiser", Brand = "MediClean", Category = ProductCategory.BathAndBody, Price = 12m, OldPrice = 16m, Stock = 95, Label = ProductLabel.OnSale, Description = "Quick-dry sanitiser gel for everyday protection." }),

        WithImage(new() { Name = "Moisturising Cream", Brand = "CeraVe", Category = ProductCategory.Beauty, Price = 52m, Stock = 38, Label = ProductLabel.Bestseller, Description = "Hydrating cream with ceramides and hyaluronic acid." }),
        WithImage(new() { Name = "Niacinamide 10% Serum", Brand = "GlowVital", Category = ProductCategory.Beauty, Price = 39m, OldPrice = 55m, Stock = 44, Label = ProductLabel.ThirtyOff, Description = "Targets blemishes and minimises pores." }),
        WithImage(new() { Name = "SPF 50 Sunscreen", Brand = "SunGuard", Category = ProductCategory.Beauty, Price = 42m, Stock = 52, Label = ProductLabel.Bestseller, Description = "Lightweight broad-spectrum SPF 50, no white cast." }),
        WithImage(new() { Name = "Gentle Facial Cleanser", Brand = "CeraVe", Category = ProductCategory.Beauty, Price = 46m, Stock = 35, Label = ProductLabel.NewArrival, Description = "Non-stripping cleanser for normal to dry skin." }),
        WithImage(new() { Name = "Hyaluronic Acid Serum", Brand = "GlowVital", Category = ProductCategory.Beauty, Price = 49m, OldPrice = 62m, Stock = 30, Label = ProductLabel.OnSale, Description = "Hydrating serum for plump, dewy-looking skin." }),
        WithImage(new() { Name = "Retinol Night Cream", Brand = "DermaSoft", Category = ProductCategory.Beauty, Price = 65m, Stock = 22, Label = ProductLabel.Bestseller, Description = "Night cream with gentle retinol and peptides." }),

        WithImage(new() { Name = "Hydra Baby Lotion", Brand = "Mustela", Category = ProductCategory.BabyCare, Price = 45m, Stock = 29, Label = ProductLabel.NewArrival, Description = "Plant-based daily lotion for delicate baby skin." }),
        WithImage(new() { Name = "Baby Dry Pants M", Brand = "Pampers", Category = ProductCategory.BabyCare, Price = 55m, Stock = 64, Label = ProductLabel.Buy1Free1, Description = "Up to 12 hours of breathable dryness, 60 pcs." }),
        WithImage(new() { Name = "Baby Gentle Shampoo", Brand = "Mustela", Category = ProductCategory.BabyCare, Price = 29m, Stock = 42, Label = ProductLabel.None, Description = "Tear-free shampoo for soft baby hair." }),
        WithImage(new() { Name = "Sensitive Baby Wipes", Brand = "Pampers", Category = ProductCategory.BabyCare, Price = 22m, OldPrice = 28m, Stock = 88, Label = ProductLabel.OnSale, Description = "Fragrance-free wipes for sensitive baby skin." }),
        WithImage(new() { Name = "Nappy Rash Cream", Brand = "TinyCare", Category = ProductCategory.BabyCare, Price = 34m, Stock = 31, Label = ProductLabel.NewArrival, Description = "Protective cream to soothe nappy rash." }),

        WithImage(new() { Name = "Blood Pressure Monitor", Brand = "Omron", Category = ProductCategory.HealthDevices, Price = 185m, Stock = 8, Label = ProductLabel.Bestseller, Description = "Automatic upper-arm monitor, stores 60 readings." }),
        WithImage(new() { Name = "Digital Thermometer", Brand = "MediCheck", Category = ProductCategory.HealthDevices, Price = 35m, Stock = 55, Label = ProductLabel.None, Description = "Fast 10-second readings with fever alarm." }),
        WithImage(new() { Name = "Fingertip Pulse Oximeter", Brand = "MediCheck", Category = ProductCategory.HealthDevices, Price = 89m, Stock = 0, Label = ProductLabel.NewArrival, Description = "Measures blood oxygen (SpO2) and pulse rate." }),
        WithImage(new() { Name = "Infrared Forehead Thermometer", Brand = "MediCheck", Category = ProductCategory.HealthDevices, Price = 79m, Stock = 18, Label = ProductLabel.NewArrival, Description = "Contactless forehead readings in one second." }),
        WithImage(new() { Name = "Glucose Monitoring Kit", Brand = "AccuLife", Category = ProductCategory.HealthDevices, Price = 129m, OldPrice = 149m, Stock = 12, Label = ProductLabel.OnSale, Description = "Starter kit for daily blood glucose checks." }),
        WithImage(new() { Name = "Portable Nebulizer Machine", Brand = "BreatheEasy", Category = ProductCategory.HealthDevices, Price = 158m, Stock = 9, Label = ProductLabel.Bestseller, Description = "Compact nebulizer for home respiratory care." }),
    ];

    private static async Task ApplySampleProductImagesAsync(ApplicationDbContext db)
    {
        var products = await db.Products.ToListAsync();
        var changed = false;
        foreach (var product in products)
        {
            var imageUrl = ProductImageCatalog.GetImageUrl(product.Name);
            if (imageUrl is null)
            {
                continue;
            }

            if (product.ImageUrl == imageUrl)
            {
                continue;
            }

            product.ImageUrl = imageUrl;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }
}
