using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Data;
using SecureTodo.Models;
using SecureTodo.Services;

namespace SecureTodo.Pages.Todos;

// Задание 2: создавать задачи могут только вошедшие пользователи
[Authorize]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db) => _db = db;

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
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var userId = User.GetUserId();
        if (userId is null)
            return Forbid();

        _db.Tasks.Add(new TaskItem
        {
            Title = Input.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Задача создана.";
        return RedirectToPage("/Todos/Index");
    }
}
