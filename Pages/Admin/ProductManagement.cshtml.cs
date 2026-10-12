using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Pages.Delivery;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Healthcare_Industry.Pages.Admin;

public class ProductManagementModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ICloudStorageService _cloudStorage;

    public ProductManagementModel(ApplicationDbContext db, ICloudStorageService cloudStorage)
    {
        _db = db;
        _cloudStorage = cloudStorage;
    }

    public List<Product> Products { get; set; } = new();

    [BindProperty]
    public ProductInput Input { get; set; } = new();

    // True when the form is editing an existing product
    public bool IsEditing => Input.Id != 0;

    public class ProductInput
    {
        public int Id { get; set; }

        [Required] public string Name { get; set; } = "";
        [Required] public string Brand { get; set; } = "";
        public ProductCategory Category { get; set; }
        public string Description { get; set; } = "";

        [Range(0, 1000000, ErrorMessage = "Price must be 0 or more.")]
        public decimal Price { get; set; }

        public decimal? OldPrice { get; set; }

        [Range(0, 1000000, ErrorMessage = "Stock must be 0 or more.")]
        public int Stock { get; set; }

        public ProductLabel Label { get; set; } = ProductLabel.None;
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

    public async Task OnGetAsync(int? editId)
    {
        await LoadProductsAsync();

        // If an editId was passed, load that product into the form
        if (editId is not null)
        {
            var p = await _db.Products.FindAsync(editId);
            if (p is not null)
            {
                Input = new ProductInput
                {
                    Id = p.Id,
                    Name = p.Name,
                    Brand = p.Brand,
                    Category = p.Category,
                    Description = p.Description,
                    Price = p.Price,
                    OldPrice = p.OldPrice,
                    Stock = p.Stock,
                    Label = p.Label,
                    ImageUrl = p.ImageUrl
                };
            }
        }
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadProductsAsync();
            return Page();
        }

        string? uploadedImageUrl;
        try
        {
            uploadedImageUrl = await _cloudStorage.SaveProductImageAsync(Input.ImageFile);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Input.ImageFile", ex.Message);
            await LoadProductsAsync();
            return Page();
        }

        if (Input.Id == 0)
        {
            // Create new product
            _db.Products.Add(new Product
            {
                Name = Input.Name,
                Brand = Input.Brand,
                Category = Input.Category,
                Description = Input.Description,
                Price = Input.Price,
                OldPrice = Input.OldPrice,
                Stock = Input.Stock,
                Label = Input.Label,
                ImageUrl = uploadedImageUrl ?? Input.ImageUrl
            });
        }
        else
        {
            // Update existing product
            var p = await _db.Products.FindAsync(Input.Id);
            if (p is not null)
            {
                p.Name = Input.Name;
                p.Brand = Input.Brand;
                p.Category = Input.Category;
                p.Description = Input.Description;
                p.Price = Input.Price;
                p.OldPrice = Input.OldPrice;
                p.Stock = Input.Stock;
                p.Label = Input.Label;
                p.ImageUrl = uploadedImageUrl ?? Input.ImageUrl;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();   // reload clean (PRG pattern)
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is not null)
        {
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    private async Task LoadProductsAsync()
        => Products = await _db.Products
                               .OrderByDescending(p => p.CreatedAt)
                               .ToListAsync();
}
