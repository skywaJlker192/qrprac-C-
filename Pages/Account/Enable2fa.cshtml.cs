using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using SecureTodo.Models;

namespace SecureTodo.Pages.Account;

/// <summary>Доп. задание (средний уровень): включение 2FA через приложение-аутентификатор + QR-код.</summary>
[Authorize]
public class Enable2faModel : PageModel
{
    private const string Issuer = "SecureTodo";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public Enable2faModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string SharedKey { get; private set; } = string.Empty;
    public string QrCodeDataUri { get; private set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Введите код из приложения")]
        [StringLength(8, MinimumLength = 6, ErrorMessage = "Код состоит из 6 цифр")]
        [Display(Name = "Код из приложения")]
        public string Code { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();
        if (user.TwoFactorEnabled) return RedirectToPage("./Profile");

        await LoadSharedKeyAndQrAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();
        if (user.TwoFactorEnabled) return RedirectToPage("./Profile");

        if (!ModelState.IsValid)
        {
            await LoadSharedKeyAndQrAsync(user);
            return Page();
        }

        var code = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
        {
            ModelState.AddModelError("Input.Code", "Неверный код. Проверьте время на телефоне и попробуйте снова.");
            await LoadSharedKeyAndQrAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _signInManager.RefreshSignInAsync(user);   // security stamp изменился — обновляем cookie

        TempData["Success"] = "Двухфакторная аутентификация включена!";
        return RedirectToPage("./Profile");
    }

    private async Task LoadSharedKeyAndQrAsync(AppUser user)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
            await _signInManager.RefreshSignInAsync(user);
        }

        key ??= string.Empty;
        SharedKey = FormatKey(key);

        // Формат otpauth:// совместим с Google Authenticator и Microsoft Authenticator
        var label = $"{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(user.Email ?? user.UserName ?? "user")}";
        var uri = $"otpauth://totp/{label}?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(10);
        QrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(png);
    }

    private static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        var pos = 0;
        while (pos + 4 < key.Length)
        {
            sb.Append(key.AsSpan(pos, 4)).Append(' ');
            pos += 4;
        }
        if (pos < key.Length)
            sb.Append(key.AsSpan(pos));
        return sb.ToString().ToLowerInvariant();
    }
}
