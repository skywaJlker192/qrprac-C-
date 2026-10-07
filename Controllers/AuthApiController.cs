using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SecureTodo.Models;
using SecureTodo.Services;

namespace SecureTodo.Controllers;

/// <summary>Задание 4 + доп. задание (refresh-токены).</summary>
[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly TokenService _tokens;

    public AuthApiController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, TokenService tokens)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokens = tokens;
    }

    /// <summary>Логин: e-mail + пароль → access (JWT) + refresh.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { error = "Неверный e-mail или пароль." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return Unauthorized(new { error = "Аккаунт временно заблокирован. Попробуйте позже." });
        if (!result.Succeeded)
            return Unauthorized(new { error = "Неверный e-mail или пароль." });

        // Если включена 2FA — без кода токен не выдаём
        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                return Unauthorized(new { error = "Требуется код двухфакторной аутентификации.", requiresTwoFactor = true });

            var code = request.TwoFactorCode.Replace(" ", "").Replace("-", "");
            var valid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);
            if (!valid)
                return Unauthorized(new { error = "Неверный код двухфакторной аутентификации.", requiresTwoFactor = true });
        }

        return Ok(await _tokens.IssueTokensAsync(user));
    }

    /// <summary>Обмен refresh-токена на новую пару (старый refresh отзывается — ротация).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var response = await _tokens.RotateAsync(request.RefreshToken);
        if (response is null)
            return Unauthorized(new { error = "Refresh-токен недействителен или отозван. Войдите заново." });

        return Ok(response);
    }

    /// <summary>Выход: отзывает переданный refresh-токен.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        var revoked = await _tokens.RevokeAsync(request.RefreshToken);
        return Ok(new { revoked });
    }

    /// <summary>Выйти на всех устройствах: отзывает ВСЕ refresh-токены пользователя.</summary>
    [HttpPost("logout-all")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var count = await _tokens.RevokeAllAsync(userId);
        return Ok(new { revokedTokens = count });
    }
}
