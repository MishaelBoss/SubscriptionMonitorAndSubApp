namespace SubApp.Scripts;

public record ParsingProgressMessage(int MailboxId, int Processed, int Total, string Status, string? Log);