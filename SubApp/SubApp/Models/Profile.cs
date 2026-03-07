using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("accounts_profile")]
public class Profile
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; }
    [Column("avatar")] public string? AvatarPath { get; set; }
    [Column("phone")] public string Phone { get; set; } = string.Empty;
    [Column("email_notifications")] public bool EmailNotifications { get; set; } = true;
    [Column("push_notifications")] public bool PushNotifications { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
