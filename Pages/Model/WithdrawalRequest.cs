namespace Healthcare_Industry.Models;

public enum WithdrawalStatus { Pending, Paid, Rejected }

public class WithdrawalRequest
{
    public int Id { get; set; }
    public string RiderId { get; set; } = "";
    public decimal Amount { get; set; }
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? AdminNote { get; set; }
}