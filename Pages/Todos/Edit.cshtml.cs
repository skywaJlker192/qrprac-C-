using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Data;

namespace SecureTodo.Pages.Todos;

[Authorize]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationService _auth;

    public EditModel(AppDbContext db, IAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Введите название")]
        [StringLength(200, ErrorMessage = "Не длиннее 200 символов")]
        [Display(Name = "Название")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Не длиннее 2000 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Выполнена")]
        public bool IsCompleted { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        // Resource-based авторизация: чужая задача → 403 Forbidden
        var result = await _auth.AuthorizeAsync(User, task, "CanEditTask");
        if (!result.Succeeded) return Forbid();

        Input = new InputModel
        {
            Title = task.Title,
            Description = task.Description,
            IsCompleted = task.IsCompleted
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        // Проверяем права и на POST — прямой запрос в обход формы тоже не пройдёт
        var result = await _auth.AuthorizeAsync(User, task, "CanEditTask");
        if (!result.Succeeded) return Forbid();

        if (!ModelState.IsValid)
            return Page();

        task.Title = Input.Title.Trim();
        task.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        task.IsCompleted = Input.IsCompleted;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Задача обновлена.";
        return RedirectToPage("/Todos/Index");
    }
}
