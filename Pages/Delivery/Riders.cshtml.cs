using Healthcare_Industry.Data;
using Healthcare_Industry.Models;
using Healthcare_Industry.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_Industry.Pages.Delivery
{
    [Authorize(Roles = "Delivery")]
    public class RidersModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public RidersModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public List<RiderSummary> Riders { get; set; } = new();
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }

        public class RiderSummary
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public int TotalDelivered { get; set; }
            public decimal TotalEarned { get; set; }
            public bool IsLocked { get; set; }
            public List<WithdrawalRequest> PendingWithdrawals { get; set; } = new();
            public List<DeliveryHistoryItem> DeliveryHistory { get; set; } = new();
        }

        public class DeliveryHistoryItem
        {
            public int OrderId { get; set; }
            public string CustomerName { get; set; } = "";
            public string DeliveryAddress { get; set; } = "";
            public DateTime? DeliveredAt { get; set; }
            public decimal Earning { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.IsDeliveryAdmin())
                return RedirectToPage("/Delivery/AssignOrders");

            await AutoHandleStaleOrdersAsync();
            await LoadRidersAsync();
            return Page();
        }

        private async Task AutoHandleStaleOrdersAsync()
        {
            var cutoff = DateTime.UtcNow.AddDays(-3);
            var staleOrders = await _db.Orders
                .Where(o => o.AssignedDeliveryId == null
                    && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Packed)
                    && o.OrderDate <= cutoff)
                .ToListAsync();

            if (!staleOrders.Any()) return;

            var admin = await _userManager.FindByEmailAsync(DeliveryRoleHelper.AdminEmail);
            if (admin is null) return;

            foreach (var order in staleOrders)
            {
                order.AssignedDeliveryId = admin.Id;
                order.DeliveryNote = "No rider claimed this within 3 days — handled by Delivery Admin.";
            }
            await _db.SaveChangesAsync();
        }

        private async Task LoadRidersAsync()
        {
            Riders = new();

            var riderIds = await _db.Orders
                .Where(o => o.AssignedDeliveryId != null)
                .Select(o => o.AssignedDeliveryId!)
                .Distinct()
                .ToListAsync();

            if (!riderIds.Any()) return;

            var users = await _db.Users
                .Where(u => riderIds.Contains(u.Id))
                .ToListAsync();

            var deliveredOrders = await _db.Orders
                .Where(o => o.AssignedDeliveryId != null
                    && riderIds.Contains(o.AssignedDeliveryId!)
                    && o.Status == OrderStatus.Delivered)
                .ToListAsync();

            var pendingWithdrawals = await _db.WithdrawalRequests
                .Where(w => riderIds.Contains(w.RiderId) && w.Status == WithdrawalStatus.Pending)
                .OrderBy(w => w.RequestedAt)
                .ToListAsync();

            foreach (var u in users)
            {
                var myDelivered = deliveredOrders.Where(o => o.AssignedDeliveryId == u.Id).ToList();
                Riders.Add(new RiderSummary
                {
                    Id = u.Id,
                    Name = string.IsNullOrWhiteSpace(u.FullName) ? "(No name set)" : u.FullName,
                    Phone = u.PhoneNumber,
                    Email = u.Email,
                    TotalDelivered = myDelivered.Count,
                    TotalEarned = myDelivered.Sum(o => o.DeliveryEarning),
                    IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    PendingWithdrawals = pendingWithdrawals.Where(w => w.RiderId == u.Id).ToList(),
                    DeliveryHistory = myDelivered
                        .OrderByDescending(o => o.DeliveredAt ?? o.OrderDate)
                        .Select(o => new DeliveryHistoryItem
                        {
                            OrderId = o.Id,
                            CustomerName = o.CustomerName,
                            DeliveryAddress = o.DeliveryAddress,
                            DeliveredAt = o.DeliveredAt,
                            Earning = o.DeliveryEarning
                        })
                        .ToList()
                });
            }

            Riders = Riders.OrderByDescending(r => r.TotalDelivered).ToList();
        }

        public async Task<IActionResult> OnPostMarkPaidAsync(int withdrawalId)
        {
            if (!User.IsDeliveryAdmin())
                return RedirectToPage("/Delivery/AssignOrders");

            var w = await _db.WithdrawalRequests.FindAsync(withdrawalId);
            if (w is not null)
            {
                w.Status = WithdrawalStatus.Paid;
                w.PaidAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditRiderAsync(string riderId, string fullName, string phoneNumber)
        {
            if (!User.IsDeliveryAdmin())
                return RedirectToPage("/Delivery/AssignOrders");

            var user = await _userManager.FindByIdAsync(riderId);
            if (user is null)
            {
                Message = "Rider not found.";
                IsSuccess = false;
                await LoadRidersAsync();
                return Page();
            }

            user.FullName = string.IsNullOrWhiteSpace(fullName) ? user.FullName : fullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

            var result = await _userManager.UpdateAsync(user);
            Message = result.Succeeded ? "Rider details updated." : "Could not update rider details.";
            IsSuccess = result.Succeeded;

            await LoadRidersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostToggleLockAsync(string riderId)
        {
            if (!User.IsDeliveryAdmin())
                return RedirectToPage("/Delivery/AssignOrders");

            var user = await _userManager.FindByIdAsync(riderId);
            if (user is null) return RedirectToPage();

            var isCurrentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            await _userManager.SetLockoutEnabledAsync(user, true);
            if (isCurrentlyLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null); // unlock
                Message = $"{user.FullName} has been unblocked and can log in again.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // lock indefinitely
                Message = $"{user.FullName} has been blocked from logging in.";
            }
            IsSuccess = true;

            await LoadRidersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string riderId, string newPassword)
        {
            if (!User.IsDeliveryAdmin())
                return RedirectToPage("/Delivery/AssignOrders");

            var user = await _userManager.FindByIdAsync(riderId);
            if (user is null) return RedirectToPage();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                Message = "Enter a new password.";
                IsSuccess = false;
                await LoadRidersAsync();
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            Message = result.Succeeded
                ? $"{user.FullName}'s password has been reset."
                : "Could not reset password: " + string.Join(" ", result.Errors.Select(e => e.Description));
            IsSuccess = result.Succeeded;

            await LoadRidersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteRiderAsync(string riderId)
        {
            if (!User.IsDeliveryAdmin())
                return RedirectToPage("/Delivery/AssignOrders");

            var user = await _userManager.FindByIdAsync(riderId);
            if (user is null) return RedirectToPage();

            var name = user.FullName;
            var result = await _userManager.DeleteAsync(user);

            Message = result.Succeeded
                ? $"{name}'s account has been permanently deleted. Their past orders remain in the system for records."
                : $"Could not delete {name}'s account.";
            IsSuccess = result.Succeeded;

            await LoadRidersAsync();
            return Page();
        }
    }
}