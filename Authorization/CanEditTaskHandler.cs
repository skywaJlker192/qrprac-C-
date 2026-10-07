using Microsoft.AspNetCore.Authorization;
using SecureTodo.Models;
using SecureTodo.Services;

namespace SecureTodo.Authorization;

/// <summary>
/// Resource-based handler: редактировать/удалять задачу может её владелец или Admin.
/// Работает и для cookie-пользователя (Razor Pages), и для JWT (API).
/// </summary>
public class CanEditTaskHandler : AuthorizationHandler<CanEditTaskRequirement, TaskItem>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanEditTaskRequirement requirement,
        TaskItem resource)
    {
        var userId = context.User.GetUserId();

        if (context.User.IsInRole(AppRoles.Admin) ||
            (!string.IsNullOrEmpty(userId) && resource.UserId == userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
