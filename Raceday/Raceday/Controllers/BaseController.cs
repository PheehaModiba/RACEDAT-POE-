using Microsoft.AspNetCore.Mvc;
using System.Net;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class BaseController : Controller
{
    protected readonly ApiClient Api;
    protected BaseController(ApiClient api) => Api = api;

    protected SessionUser? CurrentUser => HttpContext.Session.GetUser();

    /// <summary>Use when you will RENDER a view this request: shows the error on this page.</summary>
    protected bool Failed<T>(ApiResult<T> r)
    {
        if (r.Ok) return false;
        ViewData["Error"] = r.Error;
        if (r.Status == HttpStatusCode.Unauthorized) { Api.ForgetApiSession(); HttpContext.Session.Clear(); }
        return true;
    }

    /// <summary>Use when you will REDIRECT next: the message survives via TempData.</summary>
    protected bool FailedFlash<T>(ApiResult<T> r)
    {
        if (r.Ok) return false;
        TempData["Error"] = r.Error;
        if (r.Status == HttpStatusCode.Unauthorized) { Api.ForgetApiSession(); HttpContext.Session.Clear(); }
        return true;
    }
}
