using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using System.Text.Json;
using System.Globalization;

namespace RaceDay.Web.Controllers;

[SessionAuthorize("Participant")]
public sealed class ParticipantController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? search, string? type, string? location, DateTime? from, DateTime? to, bool upcomingOnly = true)
    {
        var query = new Dictionary<string, string?>
        {
            ["search"] = search,
            ["type"] = type,
            ["location"] = location,
            ["from"] = from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["to"] = to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["upcomingOnly"] = upcomingOnly.ToString().ToLowerInvariant()
        };
        var eventsTask = api.GetAsync<List<EventViewModel>>("api/events", query);
        var categoriesTask = api.GetAsync<List<CategoryViewModel>>("api/categories");
        var results = await api.GetAsync<List<ResultViewModel>>("api/results/me");
        await Task.WhenAll(eventsTask, categoriesTask);
        var events = eventsTask.Result;
        if (categoriesTask.Result.Value is not null && events.Value is not null)
            foreach (var e in events.Value)
                e.Categories = categoriesTask.Result.Value.Where(c => c.EventId == e.EventId).ToList();
        ViewBag.Registrations = await GetMyRegistrations();
        ViewBag.Results = results.Value ?? [];
        ViewBag.ApiError = events.Error ?? results.Error;
        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.Location = location;
        ViewBag.From = from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ViewBag.To = to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ViewBag.UpcomingOnly = upcomingOnly;
        return View(events.Value ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> MyEvents()
    {
        return View(await GetMyRegistrations());
    }

    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var result = await api.GetAsync<List<ResultViewModel>>("api/results/me");
        ViewBag.ApiError = result.Error;
        return View(result.Value ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await api.GetAsync<EventViewModel>($"api/events/{id}");
        if (!result.Succeeded || result.Value is null)
        {
            TempData["Notice"] = result.Error ?? "Event unavailable.";
            return RedirectToAction(nameof(MyEvents));
        }
        return View("~/Views/Home/Details.cshtml", result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> ResultDetails(int id)
    {
        var result = await api.GetAsync<List<ResultViewModel>>("api/results/me");
        var item = result.Value?.FirstOrDefault(r => r.ResultId == id);
        if (!result.Succeeded || item is null)
        {
            TempData["Notice"] = result.Error ?? "Result unavailable.";
            return RedirectToAction(nameof(MyResults));
        }
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegistrationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Notice"] = "Choose a valid event category.";
            return RedirectToAction(nameof(Index));
        }
        var result = await api.PostAsync<RegistrationFormViewModel, RegistrationViewModel>("api/enrolments", model);
        if (result.Succeeded && result.Value is not null)
        {
            var ids = GetRegistrationIds();
            if (!ids.Contains(result.Value.RegistrationId))
            {
                ids.Add(result.Value.RegistrationId);
                HttpContext.Session.SetString("RegistrationIds", JsonSerializer.Serialize(ids));
            }
        }
        TempData["Notice"] = result.Succeeded ? "You are registered for the event." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await api.DeleteAsync<object>($"api/enrolments/{id}");
        if (result.Succeeded)
        {
            var ids = GetRegistrationIds();
            ids.Remove(id);
            HttpContext.Session.SetString("RegistrationIds", JsonSerializer.Serialize(ids));
        }
        TempData["Notice"] = result.Succeeded ? "Registration cancelled." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private List<int> GetRegistrationIds()
    {
        try
        {
            return JsonSerializer.Deserialize<List<int>>(HttpContext.Session.GetString("RegistrationIds") ?? "[]") ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        }

        private async Task<List<RegistrationViewModel>> GetMyRegistrations()
        {
            var myRegistrations = await api.GetAsync<List<RegistrationViewModel>>("api/registrations/me");
            if (myRegistrations.Succeeded && myRegistrations.Value is not null)
                return myRegistrations.Value;

            var registrations = new List<RegistrationViewModel>();
            foreach (var registrationId in GetRegistrationIds())
            {
                var result = await api.GetAsync<RegistrationViewModel>($"api/registrations/{registrationId}");
                if (result.Succeeded && result.Value is not null)
                    registrations.Add(result.Value);
            }
            return registrations;
    }
}
