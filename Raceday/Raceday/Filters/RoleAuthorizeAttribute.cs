using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel;

namespace RaceDay.Web.Filters;

[AttributeUsage(AttributeTargets.Action, AllowMultiple = false)]
public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private string v;

    public RoleAuthorizeAttribute(string v)
    {
        this.v = v;
    }

    public string? Role { get; set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (Role != null && context.HttpContext.User?.Identity?.Name != Role)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}