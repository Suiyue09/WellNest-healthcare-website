using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_Industry.Pages.Delivery
{
    [Authorize(Roles = "Delivery")]
    public class PayoutModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PayoutModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public decimal TotalEarned { get; set; }
        public decimal AlreadyRequested { get; set; }
        public decimal AvailableBalance { get; set; }
        public List<WithdrawalRequest> MyWithdrawals { get; set; } = new();

        public string? Message { get; set; }
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            await LoadDataAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostRequestWithdrawalAsync(decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            await LoadDataAsync(user.Id);

            if (amount <= 0)
            {
                Message = "Enter a valid withdrawal amount.";
                IsSuccess = false;
                return Page();
            }

            if (amount > AvailableBalance)
            {
                Message = $"You can only withdraw up to RM{AvailableBalance:0.00} (your available balance).";
                IsSuccess = false;
                return Page();
            }

            _db.WithdrawalRequests.Add(new WithdrawalRequest
            {
                RiderId = user.Id,
                Amount = amount,
                Status = WithdrawalStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            Message = $"Withdrawal request for RM{amount:0.00} submitted. It will be reviewed and paid out soon.";
            IsSuccess = true;
            await LoadDataAsync(user.Id);
            return Page();
        }

        private async Task LoadDataAsync(string riderId)
        {
            var myDelivered = await _db.Orders
                .Where(o => o.AssignedDeliveryId == riderId && o.Status == OrderStatus.Delivered)
                .ToListAsync();

            TotalEarned = myDelivered.Sum(o => o.DeliveryEarning);

            MyWithdrawals = await _db.WithdrawalRequests
                .Where(w => w.RiderId == riderId)
                .OrderByDescending(w => w.RequestedAt)
                .ToListAsync();

            // Money already claimed (pending review) or already paid out shouldn't be withdrawable again
            AlreadyRequested = MyWithdrawals
                .Where(w => w.Status != WithdrawalStatus.Rejected)
                .Sum(w => w.Amount);

            AvailableBalance = Math.Max(0, TotalEarned - AlreadyRequested);
        }
    }
}