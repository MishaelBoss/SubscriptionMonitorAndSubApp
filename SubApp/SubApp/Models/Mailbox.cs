using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubApp.Models;

[Table("mail_mailbox")]
public class Mailbox
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("email")] public string Email { get; set; }
    [Column("imap_server")] public string ImapServer { get; set; }
    [Column("imap_port")] public int ImapPort { get; set; } = 993;
    [Column("password_encrypted")] public string PasswordEncrypted { get; set; }
    [Column("search_criteria")] public string SearchCriteria { get; set; }
}
