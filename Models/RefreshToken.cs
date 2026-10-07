using System.ComponentModel.DataAnnotations.Schema;

namespace SecureTodo.Models;

/// <summary>Refresh-токен (Доп. задание — сложный уровень).</summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }

    [NotMapped] public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    [NotMapped] public bool IsActive => RevokedAt == null && !IsExpired;
}
