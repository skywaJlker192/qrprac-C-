using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureTodo.Data;
using SecureTodo.Models;
using SecureTodo.Services;

namespace SecureTodo.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public ProfileModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, AppDbContext db, TokenService tokens)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _tokens = tokens;
    }

    public AppUser Account { get; private set; } = new();
    public IList<string> Roles { get; private set; } = new List<string>();
    public bool TwoFactorEnabled { get; private set; }
    public int TaskCount { get; private set; }
    public int ActiveRefreshTokens { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadAsync() ? Page() : NotFound();
    }

    /// <summary>Отключить 2FA и сбросить ключ аутентификатора.</summary>
    public async Task<IActionResult> OnPostDisable2faAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _signInManager.RefreshSignInAsync(user);   // обновляем cookie после смены security stamp

        TempData["Success"] = "Двухфакторная аутентификация отключена.";
        return RedirectToPage();
    }

    /// <summary>«Выйти на всех устройствах» — отзывает все refresh-токены API.</summary>
    public async Task<IActionResult> OnPostRevokeTokensAsync()
    {
        var userId = User.GetUserId();
        if (userId is null) return NotFound();

        var count = await _tokens.RevokeAllAsync(userId);
        TempData["Success"] = $"Отозвано refresh-токенов: {count}. На других устройствах войти по ним больше нельзя.";
        return RedirectToPage();
    }

    private async Task<bool> LoadAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return false;

        Account = user;
        Roles = await _userManager.GetRolesAsync(user);
        TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        TaskCount = await _db.Tasks.CountAsync(t => t.UserId == user.Id);

        var tokens = await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync();
        ActiveRefreshTokens = tokens.Count(t => !t.IsExpired);
        return true;
    }
}
