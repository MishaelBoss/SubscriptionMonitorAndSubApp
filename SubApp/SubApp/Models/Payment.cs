using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("subscriptions_payment")]
public class Payment
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("subscription_id")] public int SubscriptionId { get; set; }
    [ForeignKey("SubscriptionId")] public Subscription Subscription { get; set; } = null!;
    [Column("amount")] public decimal Amount { get; set; }
    [Column("currency")] public string Currency { get; set; } = "RUB";
    [Column("payment_date")] public DateTime PaymentDate { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("source")] public string Source { get; set; } = "manual";
    [Column("source_id")] public string? SourceId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}