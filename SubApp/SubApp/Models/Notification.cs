using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("subscriptions_notification")]
public class Notification
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("subscription_id")] public int? SubscriptionId { get; set; }
    [Column("notification_type")] public string NotificationType { get; set; } = null!;
    [Column("title")] public string Title { get; set; } = null!;
    [Column("message")] public string Message { get; set; } = null!;
    [Column("is_read")] public bool IsRead { get; set; }
    [Column("sent_at")] public DateTime SentAt { get; set; }
}