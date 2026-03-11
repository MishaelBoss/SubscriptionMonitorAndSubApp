using SubApp.Models;

namespace SubApp.Scripts;

public record OpenOrCloseEditUserAndProfileMessage(User? User = null, Profile? Profile = null);