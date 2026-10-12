using Healthcare_Industry.Models;

namespace Healthcare_Industry.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Brand { get; set; } = "";
    public ProductCategory Category { get; set; }
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }       // for sale pricing
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public ProductLabel Label { get; set; } = ProductLabel.None;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ProductReview> Reviews { get; set; } = new();
}
