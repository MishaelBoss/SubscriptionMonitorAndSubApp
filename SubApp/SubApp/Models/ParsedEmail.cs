using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SubApp.Models;

[Table("mail_parser_parsedemail")]
public class ParsedEmail
{
    [Key] [Column("id")] 
    [JsonPropertyName("id")] public int Id { get; set; }
    
    [NotMapped]
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [Required] [Column("mailbox_id")] 
    [JsonPropertyName("mailbox")]
    public int MailboxId { get; set; }

    [Required] [MaxLength(500)] [Column("message_id")] 
    [JsonPropertyName("message_id")] public string MessageId { get; set; }

    [Required] [MaxLength(500)] [Column("subject")] 
    [JsonPropertyName("subject")] public string Subject { get; set; }

    [Required] [EmailAddress] [Column("from_email")] 
    [JsonPropertyName("from_email")] public string FromEmail { get; set; }

    [Required] [Column("received_date")] 
    [JsonPropertyName("received_date")] public DateTime ReceivedDate { get; set; }

    [MaxLength(200)] [Column("service_name")] 
    [JsonPropertyName("service_name")] public string ServiceName { get; set; }

    [Column("amount")] 
    [JsonPropertyName("amount")] public decimal? Amount { get; set; }

    [Required] [MaxLength(3)] [Column("currency")] 
    [JsonPropertyName("currency")] public string Currency { get; set; } = "RUB";

    [Column("is_processed")] 
    [JsonPropertyName("is_processed")] public bool IsProcessed { get; set; } = false;

    [Column("processed_subscription_id")] 
    [JsonPropertyName("processed_subscription_id")] public int? ProcessedSubscriptionId { get; set; }

    [Column("raw_content")] 
    [JsonPropertyName("raw_content")] public string RawContent { get; set; }

    [Required] [Column("created_at")] 
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public Mailbox? Mailbox { get; set; } 
}
