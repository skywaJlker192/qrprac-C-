using Microsoft.AspNetCore.Identity;

namespace SecureTodo.Models;

/// <summary>Пользователь приложения (Задание 1): IdentityUser + FullName.</summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
