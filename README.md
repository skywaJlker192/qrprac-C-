# SecureTodo — «Безопасный Todo-менеджер»

Практическая работа №5. ASP.NET Core 8 · Razor Pages · EF Core (SQLite) · Identity · JWT · 2FA.

## Как запустить

1. Установить [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (подойдёт и новее — тогда поменять `TargetFramework` и версии пакетов в `SecureTodo.csproj`).
2. В папке проекта:

```bash
dotnet restore
dotnet dev-certs https --trust     # один раз, чтобы https://localhost работал без предупреждений
dotnet run --launch-profile https
```

3. Открыть **https://localhost:7080** (Swagger — `/swagger`).

База `securetodo.db` (SQLite), роли и админ создаются **автоматически** при первом запуске.
Если меняли модели — просто удалите `securetodo.db`, она создастся заново.

**Админ по умолчанию:** `admin@todo.com` / `Admin123!`
Обычного пользователя создайте через «Зарегистрироваться».

## Где что лежит (по заданиям)

| Задание | Где смотреть |
|---|---|
| **Подготовка** — модель, EF Core | `Models/TaskItem.cs`, `Models/AppUser.cs`, `Data/AppDbContext.cs` |
| **1. Identity** — настройки пароля/блокировки | `Program.cs` (блок «Задание 1»), `Pages/Account/Register.*`, `Login.*`, `Logout.*` |
| **2. Роли и защита страниц** | `Data/DbSeeder.cs` (seed ролей и админа), `Pages/Admin/Index.*` (`[Authorize(Roles="Admin")]`), `Pages/Todos/Create.cshtml.cs` (`[Authorize]`) |
| **3. Политика CanEditTask** | `Authorization/CanEditTaskRequirement.cs`, `Authorization/CanEditTaskHandler.cs`, регистрация в `Program.cs`, проверка в `Pages/Todos/Edit.cshtml.cs` и кнопки в `Pages/Todos/Index.cshtml` |
| **4. JWT API** | `Controllers/AuthApiController.cs`, `Controllers/TasksApiController.cs`, `Services/TokenService.cs`, Swagger в `Program.cs`, запросы — `SecureTodo.http` |
| **5. CSRF / XSS / заголовки** | `Program.cs` (антифоргери-фильтр, CSP и security headers), `Pages/Shared/_Layout.cshtml` (`<meta name="csrftoken">`), `wwwroot/js/site.js` (fetch с `X-CSRF-TOKEN`), AJAX-обработчик — `Pages/Todos/Index.cshtml.cs` (`OnPostToggleAsync`) |
| **Доп. (лёгкий)** — роли из админки | `Pages/Admin/Index.*`; обновление cookie — `SecurityStampValidatorOptions` в `Program.cs`; страница для проверки — `/author` |
| **Доп. (средний)** — 2FA + QR | `Pages/Account/Enable2fa.*` (QR через QRCoder), `Pages/Account/LoginWith2fa.*`, `Pages/Account/Profile.*` (включить/отключить) |
| **Доп. (сложный)** — refresh-токены | `Models/RefreshToken.cs`, `Services/TokenService.cs`, эндпоинты `/api/auth/refresh`, `/logout`, `/logout-all`, клиент — `wwwroot/js/api-client.js` |

## Как проверить контрольные точки

- **Блокировка:** 5 раз ввести неверный пароль на `/Account/Login` → аккаунт заблокирован на 15 минут.
- **403 / редирект:** обычным пользователем открыть `/admin` → 403; без входа открыть `/tasks/create` → редирект на логин.
- **Resource-based:** пользователь A создаёт задачу, B открывает `/tasks/edit/1` → 403; админ → форма.
- **JWT:** `SecureTodo.http` или Swagger (кнопка **Authorize** → вставить `accessToken`). Без токена `/api/tasks` → 401.
- **CSRF:** в консоли браузера на странице `/tasks`:
  `fetch('/tasks?handler=Toggle&id=1', {method:'POST'}).then(r => console.log(r.status))` → **400** (нет заголовка `X-CSRF-TOKEN`).
- **XSS:** создать задачу с названием `<script>alert(1)</script>` → отображается текстом, alert не срабатывает.
- **Заголовки:** DevTools → Network → Response Headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, HSTS).
- **2FA:** `Профиль → Включить 2FA` → отсканировать QR в Google Authenticator → ввести код. При следующем входе спросят код.
  Для API при включённой 2FA в `/api/auth/login` нужно передать `twoFactorCode`.
- **Refresh:** `/api/auth/login` → `/api/auth/refresh` (старый refresh после этого → 401) → `/api/auth/logout-all`.
  Клиент с авто-обновлением: вставить `wwwroot/js/api-client.js` в консоль, затем `await api.login('admin@todo.com','Admin123!')`.

## Заметки

- Access-токен живёт 15 минут (по доп. заданию); срок и секрет — в `appsettings.json` (`Jwt`). Для боевого запуска секрет нужно вынести в user-secrets / переменные окружения.
- Схема БД создаётся через `EnsureCreated` (без миграций, чтобы проект запускался «с нуля»).
- Bootstrap и иконки лежат локально в `wwwroot/lib`, т.к. CSP разрешает скрипты только с `'self'`.
- Swagger UI использует inline-скрипты, поэтому CSP на путь `/swagger` не ставится.
