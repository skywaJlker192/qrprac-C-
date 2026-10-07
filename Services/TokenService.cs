using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecureTodo.Data;
using SecureTodo.Models;

namespace SecureTodo.Services;

/// <summary>
/// Выпуск JWT (Задание 4) и refresh-токенов с ротацией и отзывом (доп. задание, сложный уровень).
/// </summary>
public class TokenService
{
    private readonly JwtSettings _settings;
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public TokenService(IOptions<JwtSettings> options, AppDbContext db, UserManager<AppUser> userManager)
    {
        _settings = options.Value;
        _db = db;
        _userManager = userManager;
    }

    /// <summary>Выдаёт пару access (JWT) + refresh и сохраняет refresh в БД.</summary>
    public async Task<AuthResponse> IssueTokensAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var now = DateTime.UtcNow;

        // claims: ID пользователя, e-mail, роли
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var refresh = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_settings.RefreshTokenDays)
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
            AccessTokenExpiresAt = expires,
            RefreshToken = refresh.Token,
            RefreshTokenExpiresAt = refresh.ExpiresAt
        };
    }

    /// <summary>Ротация: старый refresh отзывается, выдаётся новая пара. null — токен недействителен.</summary>
    public async Task<AuthResponse?> RotateAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (stored is null || !stored.IsActive)
            return null;

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user is null || await _userManager.IsLockedOutAsync(user))
            return null;

        stored.RevokedAt = DateTime.UtcNow;      // сохранится вместе с новым токеном в IssueTokensAsync
        return await IssueTokensAsync(user);
    }

    /// <summary>Отзывает один refresh-токен (выход).</summary>
    public async Task<bool> RevokeAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (stored is null || stored.RevokedAt != null)
            return false;

        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Отзывает ВСЕ активные refresh-токены пользователя.</summary>
    public async Task<int> RevokeAllAsync(string userId)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var t in tokens)
            t.RevokedAt = now;

        await _db.SaveChangesAsync();
        return tokens.Count;
    }
}
