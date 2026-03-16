using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SubApp.Models;

[Table("mail_parser_mailbox")]
public class Mailbox
{
    [Key]
    [Column("id")] 
    [JsonPropertyName("id")] public int Id { get; set; }

    [Column("user_id")] 
    [JsonPropertyName("user")] 
    public int UserId { get; set; }

    [Column("email")] 
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;

    [Column("provider")] 
    [JsonPropertyName("provider")] public string Provider { get; set; } = "other";

    [Column("imap_server")] 
    [JsonPropertyName("imap_server")] public string ImapServer { get; set; } = string.Empty;

    [Column("imap_port")] 
    [JsonPropertyName("imap_port")] public int ImapPort { get; set; } = 993;

    [Column("password_encrypted")] 
    [JsonPropertyName("password_encrypted")] public string PasswordEncrypted { get; set; } = string.Empty;

    [Column("is_active")] 
    [JsonPropertyName("is_active")] public bool IsActive { get; set; } = true;

    [Column("check_frequency")] 
    [JsonPropertyName("check_frequency")] public int CheckFrequency { get; set; } = 60;

    [Column("search_folder")] 
    [JsonPropertyName("search_folder")] public string SearchFolder { get; set; } = "INBOX";

    [Column("search_criteria")] 
    [JsonPropertyName("search_criteria")] 
    public string SearchCriteria { get; set; } = "FROM \"noreply\" OR FROM \"billing\" OR SUBJECT \"subscription\" OR SUBJECT \"payment\"";

    [Column("created_at")] 
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] 
    [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }

    [Column("last_checked")] 
    [JsonPropertyName("last_checked")] public DateTime? LastChecked { get; set; }
}
