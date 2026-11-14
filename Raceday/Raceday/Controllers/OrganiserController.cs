using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class OrganiserController : BaseController
{
    public OrganiserController(ApiClient api) : base(api) { }

    // ---------- Dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        var res = await Api.GetAsync<List<EventDto>>("api/events/mine");
        Failed(res);
        return View(new DashboardVm { Events = res.Data ?? new() });
    }

    // ---------- Events: list / create / edit / delete ----------
    public async Task<IActionResult> Events()
    {
        var res = await Api.GetAsync<List<EventDto>>("api/events/mine");
        Failed(res);
        return View((res.Data ?? new()).OrderBy(e => e.Date).ToList());
    }

    [HttpGet]
    public IActionResult Create() => View("EventForm", new EventFormVm { Date = DateTime.Today.AddDays(30).AddHours(6) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormVm vm)
    {
        if (!ValidateBanner(vm) || !ModelState.IsValid) return View("EventForm", vm);

        var res = await Api.SendFormAsync<object>(HttpMethod.Post, "api/events", ToFields(vm), "banner", vm.Banner);
        if (Failed(res)) return View("EventForm", vm);

        TempData["Success"] = $"“{vm.Name}” created. Add some categories next.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var res = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (FailedFlash(res) || res.Data == null) return RedirectToAction(nameof(Events));
        var e = res.Data;
        return View("EventForm", new EventFormVm
        {
            Id = e.Id, Name = e.Name, Description = e.Description, Date = e.Date, Location = e.Location,
            DistanceKm = e.DistanceKm, EventType = e.EventType, ExistingBannerUrl = e.BannerUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormVm vm)
    {
        vm.Id = id;
        if (!ValidateBanner(vm) || !ModelState.IsValid) return View("EventForm", vm);

        var res = await Api.SendFormAsync<object>(HttpMethod.Put, $"api/events/{id}", ToFields(vm), "banner", vm.Banner);
        if (Failed(res)) return View("EventForm", vm);

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var res = await Api.DeleteAsync<object>($"api/events/{id}");
        if (!FailedFlash(res)) TempData["Success"] = "Event deleted.";
        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------
    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var vm = await BuildCategoriesVm(eventId);
        return vm == null ? RedirectToAction(nameof(Events)) : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Categories(int eventId, CategoriesVm input)
    {
        if (!ModelState.IsValid)
        {
            var vm = await BuildCategoriesVm(eventId);
            if (vm == null) return RedirectToAction(nameof(Events));
            vm.NewName = input.NewName;
            return View(vm);
        }
        var res = await Api.PostAsync<object>($"api/events/{eventId}/categories", new { name = input.NewName.Trim() });
        if (!FailedFlash(res)) TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        var res = await Api.DeleteAsync<object>($"api/categories/{id}");
        if (!FailedFlash(res)) TempData["Success"] = "Category removed.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments for one event ----------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{eventId}");
        if (FailedFlash(ev) || ev.Data == null) return RedirectToAction(nameof(Events));
        var list = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        Failed(list);
        return View(new EventEnrolmentsVm { Event = ev.Data, Enrolments = list.Data ?? new() });
    }

    // ---------- Results entry ----------
    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var vm = await BuildResultsVm(eventId);
        return vm == null ? RedirectToAction(nameof(Events)) : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Results(int eventId, ResultsEntryVm input)
    {
        var filled = input.Rows.Where(r => !string.IsNullOrWhiteSpace(r.FinishTime) && r.Position.HasValue).ToList();

        if (input.Rows.Any(r => string.IsNullOrWhiteSpace(r.FinishTime) != !r.Position.HasValue))
            ModelState.AddModelError("", "Each runner needs both a finish time and a position (or leave both empty).");
        if (filled.GroupBy(r => r.Position).Any(g => g.Count() > 1))
            ModelState.AddModelError("", "Two participants can't have the same finishing position.");
        if (filled.Count == 0 && ModelState.ErrorCount == 0)
            ModelState.AddModelError("", "Enter at least one result before publishing.");

        if (!ModelState.IsValid)
        {
            var ev = await Api.GetAsync<EventDto>($"api/events/{eventId}");
            if (ev.Data == null) return RedirectToAction(nameof(Events));
            input.Event = ev.Data;
            return View(input);
        }

        var body = filled.Select(r => new { enrolmentId = r.EnrolmentId, finishTime = r.FinishTime, position = r.Position });
        var res = await Api.PostAsync<object>($"api/events/{eventId}/results", body);
        if (FailedFlash(res)) return RedirectToAction(nameof(Results), new { eventId });

        TempData["Success"] = "Results published. Participants can now see them.";
        return RedirectToAction(nameof(Results), new { eventId });
    }

    // ---------- helpers ----------
    private async Task<CategoriesVm?> BuildCategoriesVm(int eventId)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{eventId}");
        if (FailedFlash(ev) || ev.Data == null) return null;
        var cats = await Api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
        Failed(cats);
        return new CategoriesVm { Event = ev.Data, Categories = cats.Data ?? new() };
    }

    private async Task<ResultsEntryVm?> BuildResultsVm(int eventId)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{eventId}");
        if (FailedFlash(ev) || ev.Data == null) return null;

        var enrol = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        Failed(enrol);
        var existing = await Api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");

        var byEnrolment = (existing.Data ?? new()).ToDictionary(r => r.EnrolmentId);
        var rows = (enrol.Data ?? new())
            .Where(e => !e.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            .Select(e =>
            {
                byEnrolment.TryGetValue(e.Id, out var r);
                return new ResultRowVm
                {
                    EnrolmentId = e.Id, ParticipantName = e.ParticipantName, CategoryName = e.CategoryName,
                    FinishTime = r?.FinishTime, Position = r?.Position
                };
            }).OrderBy(r => r.Position ?? int.MaxValue).ThenBy(r => r.ParticipantName).ToList();

        return new ResultsEntryVm { Event = ev.Data, Rows = rows };
    }

    private bool ValidateBanner(EventFormVm vm)
    {
        if (vm.Banner is { Length: > 0 })
        {
            if (!vm.Banner.ContentType.StartsWith("image/") || vm.Banner.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(vm.Banner), "Upload an image (JPG/PNG) smaller than 5 MB.");
                return false;
            }
        }
        return true;
    }

    private static Dictionary<string, string?> ToFields(EventFormVm vm) => new()
    {
        ["Name"] = vm.Name,
        ["Description"] = vm.Description,
        ["Date"] = vm.Date!.Value.ToString("s", CultureInfo.InvariantCulture),
        ["Location"] = vm.Location,
        ["DistanceKm"] = vm.DistanceKm!.Value.ToString(CultureInfo.InvariantCulture),
        ["EventType"] = vm.EventType
    };
}
