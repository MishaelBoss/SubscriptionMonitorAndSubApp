using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("subscriptions_subscription")]
public class Subscription
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; }
    [Column("amount")] public decimal Amount { get; set; }
    [Column("currency")] public string Currency { get; set; } = "RUB";
    [Column("next_payment_date")] public DateTime NextPaymentDate { get; set; }
    [Column("status")] public string Status { get; set; }
    [Column("user_id")] public int UserId { get; set; }

    // Логика просрочки (аналог вашего @property в Django)
    [NotMapped]
    public bool IsOverdue => Status == "active" && NextPaymentDate < DateTime.Today;
}