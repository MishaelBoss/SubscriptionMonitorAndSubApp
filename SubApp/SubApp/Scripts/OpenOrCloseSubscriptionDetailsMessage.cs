using SubApp.Models;

namespace SubApp.Scripts;

public record OpenOrCloseSubscriptionDetailsMessage(Subscription? Sub = null);