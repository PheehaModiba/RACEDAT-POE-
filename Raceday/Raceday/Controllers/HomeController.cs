using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(ApiClient api) : base(api) { }

    // Participant home: upcoming events + filters.
    [RoleAuthorize]
    public async Task<IActionResult> Index(string? search, string? type, decimal? maxDistance)
    {
        if (CurrentUser!.IsOrganiser) return RedirectToAction("Dashboard", "Organiser");

        var vm = new EventListVm { Search = search, Type = type, MaxDistance = maxDistance };
        var res = await Api.GetAsync<List<EventDto>>("api/events");
        if (Failed(res)) return View(vm);

        // Presentation-level filtering only (what to show); no business rules here.
        var q = (res.Data ?? new()).Where(e => e.Date >= DateTime.Today);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                          || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (maxDistance.HasValue)
            q = q.Where(e => e.DistanceKm <= maxDistance.Value);

        vm.Events = q.OrderBy(e => e.Date).ToList();
        return View(vm);
    }

    // Full event information + categories + the Enter Event form.
    [RoleAuthorize]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (FailedFlash(ev) || ev.Data == null) return RedirectToAction(nameof(Index));

        var vm = new EventDetailsVm { Event = ev.Data };

        var cats = await Api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        if (!Failed(cats)) vm.Categories = cats.Data ?? new();

        if (CurrentUser!.IsParticipant)
        {
            var mine = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
            vm.AlreadyEnrolled = mine.Ok && (mine.Data ?? new()).Any(e => e.EventId == id);
        }
        return View(vm);
    }

    // "Enter Event": records participant + event + category immediately via the API.
    [HttpPost, ValidateAntiForgeryToken, RoleAuthorize("Participant")]
    public async Task<IActionResult> Enrol(EnrolVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please choose a category before entering.";
            return RedirectToAction(nameof(Details), new { id = vm.EventId });
        }

        var res = await Api.PostAsync<object>("api/enrolments", new { eventId = vm.EventId, categoryId = vm.CategoryId });
        if (FailedFlash(res)) return RedirectToAction(nameof(Details), new { id = vm.EventId });

        TempData["Success"] = "You're in! Your enrolment has been recorded.";
        return RedirectToAction("Enrolments", "Participant");
    }

    // Friendly error page (no stack traces, no raw API output).
    public IActionResult Error(int? code)
    {
        ViewData["Code"] = code;
        ViewData["Message"] = code switch
        {
            404 => "We couldn't find that page.",
            403 => "You don't have access to that page.",
            _ => "Something went wrong. Please try again."
        };
        Response.StatusCode = code ?? 500;
        return View();
    }
}
