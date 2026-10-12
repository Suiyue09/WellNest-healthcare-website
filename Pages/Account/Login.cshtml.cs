using Healthcare_Industry.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Healthcare_Industry.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is not null && await _userManager.IsInRoleAsync(user, "Admin"))
            return RedirectToPage("/Admin/Dashboard");
        if (user is not null && await _userManager.IsInRoleAsync(user, "Delivery"))
            return RedirectToPage("/Delivery/DeliveryDashboard");

        if (Url.IsLocalUrl(ReturnUrl))
            return LocalRedirect(ReturnUrl);

        return RedirectToPage("/Customer/HomePage");
    }
}
