using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SubApp.Models;

[Table("subscriptions_subscription")]
public class Subscription
{
    [Key]
    [Column("id")]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Column("uuid")]
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; set; }

    [Column("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("service_name")]
    public string? ServiceName { get; set; }

    [Column("amount")]
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [Column("currency")]
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "RUB";

    [Column("billing_cycle")]
    [JsonPropertyName("billing_cycle")]
    public string BillingCycle { get; set; } = "monthly";

    [Column("status")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [Column("billing_cycle_days")]
    [JsonPropertyName("billing_cycle_days")]
    public int BillingCycleDays { get; set; } = 30;

    [Column("start_date")]
    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; } = DateTime.Now;

    [Column("next_payment_date")]
    [JsonPropertyName("next_payment_date")]
    public DateTime NextPaymentDate { get; set; } = DateTime.Now;

    [Column("end_date")]
    [JsonPropertyName("end_date")]
    public DateTime? EndDate { get; set; }
    
    [Column("auto_renew")]
    [JsonPropertyName("auto_renew")]
    public bool AutoRenew { get; set; } = true;

    [Column("is_active")]
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [Column("user_id")]
    [JsonPropertyName("user")]
    public int UserId { get; set; }

    [Column("service_id")]
    [JsonPropertyName("service")]
    public int ServiceId { get; set; }

    [ForeignKey("ServiceId")]
    [JsonPropertyName("service_details")]
    public Service Service { get; set; } = null!;

    [Column("last_checked")]
    [JsonPropertyName("last_checked")]
    public DateTime LastChecked { get; set; } = DateTime.Now;

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    [NotMapped]
    [JsonIgnore]
    public int DaysUntilNextPayment => (NextPaymentDate.Date - DateTime.Today).Days;

    [NotMapped]
    [JsonIgnore]
    public bool IsOverdue => Status == "active" && NextPaymentDate.Date < DateTime.Today;
    
    [NotMapped]
    [JsonIgnore]
    public int OverdueDays => IsOverdue ? (DateTime.Today - NextPaymentDate.Date).Days : 0;

    [NotMapped]
    [JsonIgnore]
    public string StatusDisplay => IsOverdue ? "ПРОСРОЧЕНА" : Status switch
    {
        "active" => "Активна",
        "paused" => "Пауза",
        "trial" => "Пробная",
        _ => Status
    };

    [NotMapped]
    [JsonIgnore]
    public string StatusColor => IsOverdue ? "Red" : Status switch
    {
        "active" => "Green",
        "paused" => "Orange",
        "trial" => "Blue",
        _ => "Gray"
    };
    
    public void MarkAsPaid(DateTime? paymentDate = null)
    {
        var date = paymentDate ?? DateTime.Today;
        NextPaymentDate = BillingCycle.ToLower() switch
        {
            "monthly" => date.AddMonths(1),
            "yearly" => date.AddYears(1),
            "weekly" => date.AddDays(7),
            "custom" => date.AddDays(BillingCycleDays),
            _ => date.AddMonths(1)
        };
        UpdatedAt = DateTime.Now;
    }
}