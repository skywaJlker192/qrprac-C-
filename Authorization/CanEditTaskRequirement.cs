using Microsoft.AspNetCore.Authorization;

namespace SecureTodo.Authorization;

/// <summary>Требование политики «CanEditTask» (Задание 3).</summary>
public class CanEditTaskRequirement : IAuthorizationRequirement { }
