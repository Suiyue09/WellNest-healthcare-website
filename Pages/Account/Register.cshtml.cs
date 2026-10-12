using Healthcare_Industry.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
namespace Healthcare_Industry.Pages.Account;
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;

    private const string DeliveryAdminEmail = "Delivery@wellnest.com";

    public RegisterModel(UserManager<ApplicationUser> userManager,
                         SignInManager<ApplicationUser> signInManager,
                         IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
    }
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public string? ReturnUrl { get; set; }
    public class InputModel
    {
        [Required] public string FullName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, Phone] public string PhoneNumber { get; set; } = "";
        [Required, DataType(DataType.Password), MinLength(8)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "Password must include a capital letter, a digit, and one symbol.")]
        public string Password { get; set; } = "";
        [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
        public string? AccessCode { get; set; }   // optional — staff only
    }
    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        // Decide the role from the access code
        string role = "Customer";
        if (!string.IsNullOrWhiteSpace(Input.AccessCode))
        {
            if (Input.AccessCode == _config["RoleAccessCodes:Admin"])
                role = "Admin";
            else if (Input.AccessCode == _config["RoleAccessCodes:Delivery"])
                role = "Delivery";
            else
            {
                ModelState.AddModelError("Input.AccessCode", "Invalid access code.");
                return Page();
            }
        }

        // Riders registering with the Delivery code get a "Rider - " prefix on their
        // display name, so their identity is visually obvious everywhere (Riders roster,
        // Order Management, Customer Tracking). The one designated admin account keeps
        // their plain name. This is a display convention only — actual access control
        // to the Riders management page still checks the fixed admin email.
        bool isDeliveryAdminAccount = role == "Delivery"
            && string.Equals(Input.Email, DeliveryAdminEmail, StringComparison.OrdinalIgnoreCase);

        string displayName = (role == "Delivery" && !isDeliveryAdminAccount)
            ? $"Rider - {Input.FullName}"
            : Input.FullName;

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FullName = displayName,
            PhoneNumber = Input.PhoneNumber
        };
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return role switch
            {
                "Admin" => RedirectToPage("/Admin/Dashboard"),
                "Delivery" => RedirectToPage("/Delivery/DeliveryDashboard"),
                _ when Url.IsLocalUrl(ReturnUrl) => LocalRedirect(ReturnUrl),
                _ => RedirectToPage("/Customer/HomePage")
            };
        }
        foreach (var error in result.Errors)
        {
            var key = error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                ? "Input.Password"
                : error.Code.Contains("Email", StringComparison.OrdinalIgnoreCase) || error.Code.Contains("UserName", StringComparison.OrdinalIgnoreCase)
                    ? "Input.Email"
                    : "Input.FullName";
            ModelState.AddModelError(key, error.Description);
        }
        return Page();
    }
}
