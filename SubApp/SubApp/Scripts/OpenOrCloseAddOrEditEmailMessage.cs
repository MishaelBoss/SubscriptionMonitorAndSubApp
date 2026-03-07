namespace SubApp.Scripts;

public record OpenOrCloseAddOrEditEmailMessage(long? Id = null, 
    string? Email = null, 
    string? Password = null,
    string? ImapServer = null, 
    int? ImapPort = null, 
    int? FrequencyChecks = null);