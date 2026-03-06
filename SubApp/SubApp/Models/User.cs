using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("auth_user")]
public class User
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("username")] public string Username { get; set; } = string.Empty;
    [Column("first_name")] public string FirstName { get; set; } = string.Empty;
    [Column("last_name")] public string LastName { get; set; } = string.Empty;
    [Column("password")] public string Password { get; set; } = string.Empty;
    [Column("email")] public string Email { get; set; } = string.Empty;
    [Column("is_superuser")] public bool IsSuperuser { get; set; } = false;
    [Column("is_staff")] public bool IsStaff { get; set; } = false;
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("last_login")] public DateTime? LastLogin { get; set; }
    [Column("date_joined")] public DateTime DateJoined { get; set; } = DateTime.UtcNow;
}
