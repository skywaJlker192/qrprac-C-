using System.ComponentModel.DataAnnotations;

namespace SecureTodo.Models;

/// <summary>Задача. Один пользователь — много задач (UserId — внешний ключ).</summary>
public class TaskItem
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public AppUser? User { get; set; }
}
