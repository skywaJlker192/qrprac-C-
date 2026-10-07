using System.Security.Claims;

namespace SecureTodo.Services;

public static class ClaimsPrincipalExtensions
{
    /// <summary>ID пользователя: из cookie (NameIdentifier) или из JWT (sub).</summary>
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
