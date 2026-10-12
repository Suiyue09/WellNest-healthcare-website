using Healthcare_Industry.Models;

namespace Healthcare_Industry.Models;

public class CartItem
{
    public int Id { get; set; }

    // The logged-in user who owns this cart row (their AspNetUsers Id)
    public string UserId { get; set; } = "";

    // Which product, and how many
    public int ProductId { get; set; }
    public Product? Product { get; set; }   // lets the cart show the product's name, price, image
    public int Quantity { get; set; }
}
