using SubApp.Models;

namespace SubApp.Scripts;

public record OpenOrCloseConfirmationSubscriptionCancellationMessage(Subscription? Sub = null);