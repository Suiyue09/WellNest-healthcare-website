using Healthcare_Industry.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Healthcare_Industry.Pages.Customer;

public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [BindProperty]
    public ProfileInput Profile { get; set; } = new();

    [BindProperty]
    public PasswordInput Password { get; set; } = new();

    public string? Message { get; set; }
    public bool IsSuccess { get; set; }

    public class ProfileInput
    {
        [Required] public string FullName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string? PhoneNumber { get; set; }
        [Required] public string? Address { get; set; }
    }

    public class PasswordInput
    {
        [DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
        [DataType(DataType.Password)] public string NewPassword { get; set; } = "";
        [DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToPage("/Account/Login");

        LoadProfile(user);
        if (TempData["ProfileMessage"] is string profileMessage)
        {
            Message = profileMessage;
            IsSuccess = false;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveProfileAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToPage("/Account/Login");

        ClearPasswordModelState();
        if (!TryValidateModel(Profile, nameof(Profile)))
        {
            LoadProfile(user);
            return Page();
        }

        user.FullName = Profile.FullName.Trim();
        user.Address = Profile.Address?.Trim();
        user.PhoneNumber = Profile.PhoneNumber?.Trim();

        if (!string.Equals(user.Email, Profile.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager.SetEmailAsync(user, Profile.Email.Trim());
            if (!emailResult.Succeeded)
            {
                AddIdentityErrors(emailResult);
                return Page();
            }

            await _userManager.SetUserNameAsync(user, Profile.Email.Trim());
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return Page();
        }

        IsSuccess = true;
        Message = "Profile updated.";
        LoadProfile(user);
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToPage("/Account/Login");

        ModelState.ClearValidationState(nameof(Profile));
        if (IsPasswordChangeBlank())
        {
            LoadProfile(user);
            Message = "Password was not changed. You only need to fill this section when you want to update your password.";
            IsSuccess = true;
            return Page();
        }

        ValidatePasswordChange();
        if (!ModelState.IsValid)
        {
            LoadProfile(user);
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, Password.CurrentPassword, Password.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            LoadProfile(user);
            return Page();
        }

        IsSuccess = true;
        Message = "Password changed.";
        Password = new PasswordInput();
        LoadProfile(user);
        return Page();
    }

    private void LoadProfile(ApplicationUser user)
    {
        Profile = new ProfileInput
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        };
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
    }

    private bool IsPasswordChangeBlank() =>
        string.IsNullOrWhiteSpace(Password.CurrentPassword)
        && string.IsNullOrWhiteSpace(Password.NewPassword)
        && string.IsNullOrWhiteSpace(Password.ConfirmPassword);

    private void ValidatePasswordChange()
    {
        if (string.IsNullOrWhiteSpace(Password.CurrentPassword))
            ModelState.AddModelError("Password.CurrentPassword", "Enter your current password.");

        if (string.IsNullOrWhiteSpace(Password.NewPassword))
        {
            ModelState.AddModelError("Password.NewPassword", "Enter a new password.");
        }
        else
        {
            if (Password.NewPassword.Length < 8)
                ModelState.AddModelError("Password.NewPassword", "Password must be at least 8 characters.");
            if (!Password.NewPassword.Any(char.IsUpper))
                ModelState.AddModelError("Password.NewPassword", "Password must include a capital letter.");
            if (!Password.NewPassword.Any(char.IsDigit))
                ModelState.AddModelError("Password.NewPassword", "Password must include a digit.");
            if (!Password.NewPassword.Any(ch => !char.IsLetterOrDigit(ch)))
                ModelState.AddModelError("Password.NewPassword", "Password must include one symbol.");
        }

        if (string.IsNullOrWhiteSpace(Password.ConfirmPassword))
            ModelState.AddModelError("Password.ConfirmPassword", "Confirm your new password.");
        else if (!string.Equals(Password.NewPassword, Password.ConfirmPassword, StringComparison.Ordinal))
            ModelState.AddModelError("Password.ConfirmPassword", "Passwords do not match.");
    }

    private void ClearPasswordModelState()
    {
        ModelState.Remove("Password.CurrentPassword");
        ModelState.Remove("Password.NewPassword");
        ModelState.Remove("Password.ConfirmPassword");
        ModelState.ClearValidationState(nameof(Password));
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }
}
