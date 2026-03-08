using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("subscriptions_subscription")]
public class Subscription
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("uuid")] public Guid Uuid { get; set; }
    [Column("name")] public string Name { get; set; } = null!;
    [Column("amount")] public decimal Amount { get; set; }
    
    [Column("currency")] public string Currency { get; set; } = "RUB";
    [Column("billing_cycle")] public string BillingCycle { get; set; } = "monthly";
    [Column("status")] public string Status { get; set; } = "active";

    [Column("billing_cycle_days")] public int BillingCycleDays { get; set; }
    [Column("start_date")] public DateTime StartDate { get; set; }
    [Column("next_payment_date")] public DateTime NextPaymentDate { get; set; }
    [Column("end_date")] public DateTime? EndDate { get; set; }
    
    [Column("auto_renew")] public bool AutoRenew { get; set; }
    [Column("notes")] public string? Notes { get; set; }

    [Column("user_id")] public int UserId { get; set; }
    [ForeignKey("UserId")] public User User { get; set; } = null!;

    [Column("service_id")] public int ServiceId { get; set; }
    [ForeignKey("ServiceId")] public Service Service { get; set; } = null!;
    
    [Column("last_checked")] public DateTime LastChecked { get; set; } = DateTime.Now;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.Now;
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Column("is_active")] public bool IsActive { get; set; } = true;

    [NotMapped] public int DaysUntilNextPayment => (NextPaymentDate.Date - DateTime.Today).Days;
    [NotMapped] public bool IsOverdue => Status == "active" && NextPaymentDate < DateTime.Today;
    [NotMapped] public int OverdueDays => IsOverdue ? (DateTime.Today - NextPaymentDate).Days : 0;
    [NotMapped] public string StatusDisplay => IsOverdue ? "ПРОСРОЧЕНА" : Status switch
    {
        "active" => "Активна",
        "paused" => "Приостановлена",
        "cancelled" => "Отменена",
        _ => Status
    };
    [NotMapped] public string StatusColor => IsOverdue ? "Red" : Status switch
    {
        "active" => "Green",
        "paused" => "Orange",
        "cancelled" => "Red",
        "trial" => "Blue",
        _ => "Gray"
    };

    public bool IsExpiringSoon(int days = 7)
    {
        var diff = (NextPaymentDate - DateTime.Today).Days;
        return Status == "active" && !IsOverdue && diff >= 0 && diff <= days;
    }

    public decimal CalculateMonthlyCost()
    {
        return BillingCycle switch
        {
            "monthly" => Amount,
            "yearly" => Amount / 12,
            "quarterly" => Amount / 3,
            "weekly" => Amount * 4.33m,
            "custom" when BillingCycleDays > 0 => (Amount / BillingCycleDays) * 30,
            _ => Amount
        };
    }

    public void MarkAsPaid(DateTime? paymentDate = null)
    {
        var date = paymentDate ?? DateTime.Today;
        NextPaymentDate = BillingCycle switch
        {
            "monthly" => date.AddDays(30),
            "yearly" => date.AddYears(1),
            "quarterly" => date.AddMonths(3),
            "weekly" => date.AddDays(7),
            "custom" => date.AddDays(BillingCycleDays),
            _ => date.AddDays(30)
        };
    }
}