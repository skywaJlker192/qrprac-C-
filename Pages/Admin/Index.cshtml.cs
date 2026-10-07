using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureTodo.Models;
using SecureTodo.Services;

namespace SecureTodo.Pages.Admin;

// Задание 2: только Admin. Обычный пользователь → 403, аноним → редирект на логин.
[Authorize(Roles = "Admin")]
public class AdminIndexModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;

    public AdminIndexModel(UserManager<AppUser> userManager) => _userManager = userManager;

    public List<UserRow> Users { get; private set; } = new();

    public class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string CurrentRole { get; set; } = AppRoles.User;
    }

    public async Task OnGetAsync() => await LoadAsync();

    /// <summary>
    /// Доп. задание (лёгкий уровень): смена роли классической формой с антифоргери-токеном.
    /// </summary>
    public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string role)
    {
        if (!AppRoles.All.Contains(role))
        {
            TempData["Error"] = "Неизвестная роль.";
            return RedirectToPage();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToPage();
        }

        // Защита от случайной блокировки: нельзя снять роль Admin с самого себя
        if (user.Id == User.GetUserId() && role != AppRoles.Admin)
        {
            TempData["Error"] = "Нельзя снять роль Admin с самого себя.";
            return RedirectToPage();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removed = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removed.Succeeded)
        {
            TempData["Error"] = "Не удалось снять старые роли.";
            return RedirectToPage();
        }

        var added = await _userManager.AddToRoleAsync(user, role);
        if (!added.Succeeded)
        {
            TempData["Error"] = "Не удалось назначить новую роль.";
            return RedirectToPage();
        }

        // Пользователь получит новую роль в cookie на следующем запросе (SecurityStampValidator)
        TempData["Success"] = $"Роль пользователя {user.Email} изменена на {role}.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        foreach (var u in users)
        {
            var roles = (await _userManager.GetRolesAsync(u)).ToList();
            Users.Add(new UserRow
            {
                Id = u.Id,
                Email = u.Email ?? u.UserName ?? string.Empty,
                FullName = u.FullName,
                Roles = roles,
                CurrentRole = roles.Contains(AppRoles.Admin) ? AppRoles.Admin
                            : roles.Contains(AppRoles.Author) ? AppRoles.Author
                            : AppRoles.User
            });
        }
    }
}
