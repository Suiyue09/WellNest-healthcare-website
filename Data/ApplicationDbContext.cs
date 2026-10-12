using Healthcare_Industry.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Healthcare_Industry.Data;
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
}