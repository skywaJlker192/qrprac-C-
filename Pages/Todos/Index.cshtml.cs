using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureTodo.Data;
using SecureTodo.Models;

namespace SecureTodo.Pages.Todos;

[Authorize]
public class TasksIndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationService _auth;

    public TasksIndexModel(AppDbContext db, IAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    public List<TaskItem> Tasks { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Tasks = await _db.Tasks
            .Include(t => t.User)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// AJAX-действие «Отметить выполненной» (Задание 5).
    /// Антифоргери-токен приходит в заголовке X-CSRF-TOKEN; без него — 400 Bad Request.
    /// </summary>
    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        var result = await _auth.AuthorizeAsync(User, task, "CanEditTask");
        if (!result.Succeeded) return Forbid();

        task.IsCompleted = !task.IsCompleted;
        await _db.SaveChangesAsync();

        return new JsonResult(new { id = task.Id, isCompleted = task.IsCompleted });
    }
}
