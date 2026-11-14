using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute(params string[] roles) : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _roles = roles;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        if (string.IsNullOrEmpty(session.GetString("Role")) || session.GetInt32("UserId") is null)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        }
        else if (_roles.Length > 0 && !_roles.Contains(session.GetString("Role"), StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
        return Task.CompletedTask;
    }
}
