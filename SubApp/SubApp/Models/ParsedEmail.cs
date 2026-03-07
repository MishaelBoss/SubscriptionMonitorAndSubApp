using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("mail_parser_parsedemail")]
public class ParsedEmail
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("subject")] public string Subject { get; set; }
    [Column("amount")] public decimal? Amount { get; set; }
    [Column("received_date")] public DateTime ReceivedDate { get; set; }
    [Column("mailbox_id")] public int MailboxId { get; set; }
}
