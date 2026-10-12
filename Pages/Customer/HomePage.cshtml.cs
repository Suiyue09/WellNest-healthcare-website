using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer;

public class HomePageModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public HomePageModel(ApplicationDbContext db) => _db = db;

    // The products shown in the main grid (after any filtering)
    public List<Product> Products { get; set; } = new();

    // A few newest items for the "New arrivals" strip
    public List<Product> NewArrivals { get; set; } = new();

    // Current filter state (used to drive the heading + active category pill)
    public string? Category { get; set; }
    public string? Q { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string CustomerInitial { get; set; } = "U";

    public async Task OnGetAsync(string? category, string? q, decimal? minPrice, decimal? maxPrice)
    {
        Category = category;
        Q = q;
        MinPrice = minPrice;
        MaxPrice = maxPrice;

        // New arrivals: 4 most-recently-added products (always shown)
        NewArrivals = await _db.Products
            .OrderByDescending(p => p.Id)
            .Take(4)
            .ToListAsync();

        // Main grid: start from all products, then filter
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category)
            && Enum.TryParse<ProductCategory>(category, out var cat))
        {
            query = query.Where(p => p.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Brand.ToLower().Contains(term));
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        Products = await query.OrderBy(p => p.Name).ToListAsync();

        // Pass state to the shared layout (active pill + search box value)
        ViewData["ActiveCategory"] = string.IsNullOrWhiteSpace(category) ? "All" : category;
        ViewData["Query"] = q ?? "";

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var user = await _db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.FullName, u.Email })
                    .FirstOrDefaultAsync();
                CustomerInitial = CustomerDisplayService.GetInitial(user?.FullName, user?.Email);
            }
        }
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int productId, string? category, string? q, decimal? minPrice, decimal? maxPrice)
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToLogin(category, q, minPrice, maxPrice);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return RedirectToLogin(category, q, minPrice, maxPrice);

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.Stock > 0);
        if (product is null)
            return RedirectToPage("/Customer/HomePage", new { category, q, minPrice, maxPrice });

        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        if (item is null)
        {
            _db.CartItems.Add(new CartItem { UserId = userId, ProductId = productId, Quantity = 1 });
        }
        else if (item.Quantity < product.Stock)
        {
            item.Quantity++;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/Customer/HomePage", new { category, q, minPrice, maxPrice });
    }

    private IActionResult RedirectToLogin(string? category, string? q, decimal? minPrice, decimal? maxPrice)
    {
        var returnUrl = Url.Page("/Customer/HomePage", new { category, q, minPrice, maxPrice });
        return RedirectToPage("/Account/Login", new { returnUrl });
    }
}
