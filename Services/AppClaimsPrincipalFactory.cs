using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SecureTodo.Models;

namespace SecureTodo.Services;

/// <summary>Добавляет в cookie-claims полное имя, чтобы показывать его в шапке без запроса в БД.</summary>
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public AppClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("FullName",
            string.IsNullOrWhiteSpace(user.FullName) ? (user.Email ?? user.UserName ?? "") : user.FullName));
        return identity;
    }
}
