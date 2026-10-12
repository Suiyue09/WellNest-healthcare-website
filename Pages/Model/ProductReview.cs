namespace Healthcare_Industry.Models;

public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int OrderId { get; set; }
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
