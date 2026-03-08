using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("subscriptions_usagedata")]
public class UsageData
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("subscription_id")] public int SubscriptionId { get; set; }
    [Column("date")] public DateTime Date { get; set; }
    [Column("last_used")] public DateTime? LastUsed { get; set; }
    [Column("usage_count")] public int UsageCount { get; set; }
    [Column("notes")] public string? Notes { get; set; }
}