using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : BaseController
{
    public AccountController(ApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentUser != null) return RedirectToHome(CurrentUser);
        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // Start from a clean session so one user's API cookie never leaks to another.
        HttpContext.Session.Clear();

        var res = await Api.PostAsync<LoginResponse>("api/auth/login", new { email = vm.Email, password = vm.Password });
        if (!res.Ok || res.Data == null)
        {
            ModelState.AddModelError("", res.Ok ? "Login failed. Please try again." : res.Error);
            return View(vm);
        }

        var d = res.Data;
        var user = new SessionUser
        {
            UserId = d.UserId,
            Role = d.Role,
            FullName = !string.IsNullOrWhiteSpace(d.FullName) ? d.FullName! : $"{d.FirstName} {d.LastName}".Trim()
        };
        HttpContext.Session.SetUser(user);   // stores user id + role for every page
        Api.SaveToken(d.Token);

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return RedirectToHome(user);
    }

    [HttpGet]
    public IActionResult Register() => CurrentUser != null ? RedirectToHome(CurrentUser) : View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var res = await Api.PostAsync<object>("api/auth/register", new
        {
            firstName = vm.FirstName, lastName = vm.LastName, email = vm.Email,
            phone = vm.Phone, password = vm.Password, role = vm.Role
        });
        if (!res.Ok)
        {
            ModelState.AddModelError("", res.Error);
            return View(vm);
        }
        TempData["Success"] = "Account created. Log in to get started.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (CurrentUser != null) await Api.PostAsync<object>("api/auth/logout");   // best effort
        Api.ForgetApiSession();
        HttpContext.Session.Clear();
        TempData["Success"] = "You've been logged out.";
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToHome(SessionUser u) =>
        u.IsOrganiser ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");
}
