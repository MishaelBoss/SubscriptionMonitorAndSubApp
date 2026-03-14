using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SubApp.Models;

[Table("mail_parser_parsedemail")]
[Index(nameof(MailboxId), nameof(IsProcessed))] 
public class ParsedEmail
{
    [Key] [Column("id")] public int Id { get; set; }
    [Required] [Column("mailbox_id")] public int MailboxId { get; set; }
    [ForeignKey("MailboxId")] public virtual Mailbox Mailbox { get; set; }
    [Required] [MaxLength(500)] [Column("message_id")] public string MessageId { get; set; }
    [Required] [MaxLength(500)] [Column("subject")] public string Subject { get; set; }
    [Required] [EmailAddress] [Column("from_email")] public string FromEmail { get; set; }
    [Required] [Column("received_date")] public DateTime ReceivedDate { get; set; }
    [MaxLength(200)] [Column("service_name")] public string ServiceName { get; set; }
    [Column("amount")] public decimal? Amount { get; set; }
    [Required] [MaxLength(3)] [Column("currency")] public string Currency { get; set; } = "RUB";
    [Column("payment_date")] public DateTime? PaymentDate { get; set; }
    [Column("next_payment_date")] public DateTime? NextPaymentDate { get; set; }
    [Required] [Column("is_processed")] public bool IsProcessed { get; set; } = false;
    [Column("processed_subscription_id")] public int? ProcessedSubscriptionId { get; set; }
    [Column("error_message")] public string ErrorMessage { get; set; }
    [Column("raw_content")] public string RawContent { get; set; }
    [Required] [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
