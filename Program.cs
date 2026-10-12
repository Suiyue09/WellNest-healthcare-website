using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database now points at the AWS RDS cloud database.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CloudConnection")));

// Identity + roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie -> custom Account pages
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Role policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("DeliveryOnly", p => p.RequireRole("Delivery"));
});

// Cloud integration adapters. Demo providers are used until live API keys are configured.
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IPaymentGateway>(sp =>
{
    var options = sp.GetRequiredService<IConfiguration>().GetSection("Stripe").Get<StripeOptions>() ?? new StripeOptions();
    return IsDemoStripeKey(options.SecretKey)
        ? new DemoPaymentGateway()
        : ActivatorUtilities.CreateInstance<StripeCheckoutPaymentGateway>(sp);
});
builder.Services.AddScoped<ICustomerNotificationService, FileCustomerNotificationService>();
builder.Services.AddScoped<ICloudStorageService, LocalCloudStorageService>();
builder.Services.AddScoped<IShippingService, DemoShippingService>();
builder.Services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();

// Razor Pages + access rules
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Delivery", "DeliveryOnly");
    options.Conventions.AllowAnonymousToFolder("/Account");
});

var app = builder.Build();

// Seed roles + admin on startup
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
    await SampleDataSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

static bool IsDemoStripeKey(string? secretKey)
{
    if (string.IsNullOrWhiteSpace(secretKey))
    {
        return true;
    }

    return secretKey.Contains("your_key_here", StringComparison.OrdinalIgnoreCase)
        || secretKey.Contains("********", StringComparison.Ordinal);
}
