using System;

namespace SubApp.Models;

public class UserProfileDto
{
    public string username { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email { get; set; }
    public string phone { get; set; }
    public DateTime date_joined { get; set; }
    public bool email_notifications { get; set; }
    public bool push_notifications { get; set; }
}