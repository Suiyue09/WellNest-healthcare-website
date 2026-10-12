using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer
{
    public class ProductDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ProductDetailsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public Product? Product { get; set; }

        public List<Product> SimilarProducts { get; set; } = new();
        public List<ProductReview> Reviews { get; set; } = new();
        public string CustomerInitial { get; set; } = "U";
        public decimal AverageRating => Reviews.Count == 0 ? 0m : Math.Round((decimal)Reviews.Average(r => r.Rating), 1);

        [BindProperty, Range(1, 99)]
        public int Quantity { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (Product == null)
                return NotFound();

            await LoadCustomerInitialAsync();
            await LoadSimilarAndReviewsAsync(id);

            return Page();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int id, bool buyNow = false)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToLogin(id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToLogin(id);

            Product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (Product is null)
                return NotFound();

            await LoadCustomerInitialAsync();

            if (Product.Stock <= 0)
            {
                ModelState.AddModelError(string.Empty, "This product is currently out of stock.");
                await LoadSimilarAndReviewsAsync(id);
                return Page();
            }

            var safeQty = Math.Clamp(Quantity, 1, Product.Stock);
            var item = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == id);
            if (item is null)
            {
                _db.CartItems.Add(new CartItem { UserId = userId, ProductId = id, Quantity = safeQty });
            }
            else
            {
                item.Quantity = Math.Min(Product.Stock, item.Quantity + safeQty);
            }

            await _db.SaveChangesAsync();
            return buyNow ? RedirectToPage("/Customer/Checkout") : RedirectToPage("/Customer/ProductDetails", new { id });
        }

        private IActionResult RedirectToLogin(int id)
        {
            var returnUrl = Url.Page("/Customer/ProductDetails", new { id });
            return RedirectToPage("/Account/Login", new { returnUrl });
        }

        private async Task LoadSimilarAndReviewsAsync(int id)
        {
            if (Product is null) return;
            SimilarProducts = await _db.Products
                .Where(p => p.Category == Product.Category && p.Id != id)
                .OrderByDescending(p => p.Label == ProductLabel.Bestseller)
                .ThenBy(p => p.Name)
                .Take(4)
                .ToListAsync();

            Reviews = await _db.ProductReviews
                .AsNoTracking()
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        private async Task LoadCustomerInitialAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return;

            var user = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.FullName, u.Email })
                .FirstOrDefaultAsync();
            CustomerInitial = CustomerDisplayService.GetInitial(user?.FullName, user?.Email);
        }
    }
}
