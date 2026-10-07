using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Models;

namespace SecureTodo.Pages.Account;

/// <summary>Доп. задание (средний уровень): второй шаг входа — код из Google Authenticator.</summary>
public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;

    public LoginWith2faModel(SignInManager<AppUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool RememberMe { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите код из приложения")]
        [StringLength(8, MinimumLength = 6, ErrorMessage = "Код состоит из 6 цифр")]
        [Display(Name = "Код аутентификатора")]
        public string Code { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Пользователь, прошедший первый шаг (пароль). Если его нет — на логин.
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return RedirectToPage("./Login");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return RedirectToPage("./Login");

        var code = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var result = await _signInManager.TwoFactorSignInAsync(
            TokenOptions.DefaultAuthenticatorProvider, code, RememberMe, rememberClient: false);

        if (result.Succeeded)
        {
            return !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
                ? LocalRedirect(ReturnUrl)
                : RedirectToPage("/Index");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Аккаунт заблокирован на 15 минут из-за неверных кодов.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Неверный код. Попробуйте ещё раз.");
        return Page();
    }
}
