using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Participant")]
public class ParticipantController : BaseController
{
    public ParticipantController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Enrolments()
    {
        var res = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
        Failed(res);
        return View((res.Data ?? new()).OrderByDescending(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> Results()
    {
        var res = await Api.GetAsync<List<ResultDto>>("api/results/mine");
        Failed(res);
        return View((res.Data ?? new()).OrderByDescending(r => r.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var res = await Api.GetAsync<ProfileDto>("api/profile");
        var p = res.Data ?? new ProfileDto();
        Failed(res);
        return View(new ProfileVm
        {
            FirstName = p.FirstName, LastName = p.LastName, Email = p.Email,
            Phone = p.Phone, ProfilePictureUrl = p.ProfilePictureUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // Basic client-side sanity for the image; the API validates again.
        if (vm.Picture is { Length: > 0 })
        {
            var okType = vm.Picture.ContentType.StartsWith("image/");
            if (!okType || vm.Picture.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(vm.Picture), "Upload an image (JPG/PNG) smaller than 5 MB.");
                return View(vm);
            }
        }

        var res = await Api.PutAsync<object>("api/profile", new
        { firstName = vm.FirstName, lastName = vm.LastName, email = vm.Email, phone = vm.Phone });
        if (Failed(res)) return View(vm);

        if (vm.Picture is { Length: > 0 })
        {
            // File goes to the API, which stores it in Azure Blob Storage.
            var pic = await Api.SendFormAsync<object>(HttpMethod.Post, "api/profile/picture",
                new Dictionary<string, string?>(), "file", vm.Picture);
            if (Failed(pic)) return View(vm);
        }

        var u = CurrentUser!;
        u.FullName = $"{vm.FirstName} {vm.LastName}".Trim();
        HttpContext.Session.SetUser(u);

        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }
}
