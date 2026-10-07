using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SecureTodo.Pages;

// Страница ошибок: сохраняет настоящий HTTP-код (400/403/404/500) и красиво его показывает.
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public int Code { get; private set; } = 500;
    public string Title { get; private set; } = "Ошибка сервера";
    public string Message { get; private set; } = "Что-то пошло не так.";

    public void OnGet(int? code) => Handle(code);

    // StatusCodePages повторяет исходный метод, поэтому нужен и POST
    public void OnPost(int? code) => Handle(code);

    private void Handle(int? code)
    {
        Code = code ?? 500;
        Response.StatusCode = Code;

        (Title, Message) = Code switch
        {
            400 => ("Неверный запрос", "Запрос отклонён: нет или неверен антифоргери-токен (защита от CSRF)."),
            401 => ("Нужна авторизация", "Войдите в аккаунт, чтобы продолжить."),
            403 => ("Доступ запрещён", "У вас нет прав для просмотра этой страницы или выполнения действия."),
            404 => ("Страница не найдена", "Такой страницы не существует."),
            _ => ("Ошибка сервера", "Что-то пошло не так. Попробуйте позже.")
        };
    }
}
