namespace SecureTodo.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Author = "Author";
    public const string User = "User";

    public static readonly string[] All = { Admin, Author, User };
}
