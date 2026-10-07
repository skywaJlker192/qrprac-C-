using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SecureTodo.Pages.Author;

// Страница только для роли Author — для проверки доп. задания «смена ролей без перелогина»
[Authorize(Roles = "Author")]
public class AuthorIndexModel : PageModel
{
    public void OnGet() { }
}
