using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecureTodo.Authorization;
using SecureTodo.Data;
using SecureTodo.Models;
using SecureTodo.Services;

var builder = WebApplication.CreateBuilder(args);

// ───────────── EF Core (SQLite) ─────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ───────────── Задание 1: Identity ─────────────
builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        // пароль: минимум 6 символов, цифра обязательна
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // блокировка после 5 неудачных попыток на 15 минут
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<RussianIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

// Cookie-схема для Razor Pages
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Настоящий 403 Forbidden вместо редиректа на AccessDenied (Задания 2 и 3)
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Доп. задание (лёгкий уровень): роли в cookie обновляются на следующем запросе.
// SecurityStampValidator проверяет пользователя на каждом запросе и пересобирает claims (в т.ч. роли).
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);

// ───────────── Задание 4: JWT ─────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
          ?? throw new InvalidOperationException("Секция Jwt не найдена в appsettings.json");
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key должен быть не короче 32 символов.");

// Cookie остаётся схемой по умолчанию (Identity), а JWT ("Bearer") подключается
// к API-контроллерам через [Authorize(AuthenticationSchemes = "Bearer")].
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MapInboundClaims = false; // оставляем короткие имена claims: sub, email, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,      // просроченный токен отклоняется сразу
            NameClaimType = "email",
            RoleClaimType = "role"
        };
    });

// ───────────── Задание 3: политика и handler ─────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanEditTask", policy => policy.AddRequirements(new CanEditTaskRequirement()));
});
builder.Services.AddSingleton<IAuthorizationHandler, CanEditTaskHandler>();

builder.Services.AddScoped<TokenService>();

// ───────────── Задание 5: CSRF ─────────────
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";          // токен для AJAX читается из этого заголовка
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddRazorPages(options =>
{
    // глобальный антифоргери-фильтр для всех POST-форм Razor Pages
    options.Conventions.ConfigureFilter(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddControllers();

// ───────────── Swagger с поддержкой JWT ─────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SecureTodo API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Вставьте JWT-токен (только сам токен, без слова Bearer)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ───────────── Seed ролей и админа (Задание 2) ─────────────
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();

// ───────────── Задание 5: security headers + CSP ─────────────
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    if (context.Request.IsHttps)
    {
        // в Development — короткий срок, чтобы не «залипнуть» на https для localhost
        headers["Strict-Transport-Security"] = app.Environment.IsDevelopment()
            ? "max-age=300"
            : "max-age=31536000; includeSubDomains";
    }

    // Swagger UI использует inline-скрипты — для него CSP не ставим
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; " +
            "font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    }

    await next();
});

// Красивые страницы для 400/403/404… (кроме API и Swagger)
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api") && !ctx.Request.Path.StartsWithSegments("/swagger"),
    branch => branch.UseStatusCodePagesWithReExecute("/Error/{0}"));

app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SecureTodo API v1");
    options.DocumentTitle = "SecureTodo API";
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
