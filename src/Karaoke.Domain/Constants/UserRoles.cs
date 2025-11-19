namespace Karaoke.Domain.Constants;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";
    public const string User = "User";
    
    public static IEnumerable<string> AllRoles => new[] { Admin, Moderator, User };
}