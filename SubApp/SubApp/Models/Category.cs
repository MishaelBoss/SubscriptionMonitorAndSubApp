using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("subscriptions_category")]
public class Category
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = null!;
    [Column("icon")] public string Icon { get; set; } = "📦";
    [Column("color")] public string Color { get; set; } = "#6c757d";
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}