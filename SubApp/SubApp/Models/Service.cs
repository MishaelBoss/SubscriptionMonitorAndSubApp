using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("subscriptions_service")]
public class Service
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = null!;
    [Column("logo")] public string? Logo { get; set; }
    [Column("website")] public string? Website { get; set; }
    [Column("is_active")] public bool IsActive { get; set; }
    [Column("category_id")] public int? CategoryId { get; set; }
    [ForeignKey("CategoryId")] public Category? Category { get; set; }
}