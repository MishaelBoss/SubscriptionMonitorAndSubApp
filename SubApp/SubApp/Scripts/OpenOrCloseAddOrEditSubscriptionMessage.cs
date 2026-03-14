using SubApp.Models;

namespace SubApp.Scripts;

public record OpenOrCloseAddOrEditSubscriptionMessage(Subscription? Sub = null);