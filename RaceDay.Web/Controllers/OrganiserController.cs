using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize("Organiser")]
public sealed class OrganiserController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var events = await api.GetAsync<List<EventViewModel>>("api/events/mine");
        var mine = events.Value ?? [];
        var categories = await api.GetAsync<List<CategoryViewModel>>("api/categories");
        if (categories.Value is not null)
            foreach (var e in mine)
                e.Categories = categories.Value.Where(c => c.EventId == e.EventId).ToList();
        ViewBag.ApiError = events.Error;
        var registrations = await api.GetAsync<List<RegistrationViewModel>>("api/registrations");
        ViewBag.Registrations = registrations.Value ?? [];
        ViewBag.RegistrationError = registrations.Error;
        return View(mine);
    }

    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var (events, categories) = await GetMyEventsAndCategories();
        ViewBag.ApiError = events.Error ?? categories.Error;
        ViewBag.Events = events.Value ?? [];
        return View(categories.Value?.Where(c => events.Value?.Any(e => e.EventId == c.EventId) == true).ToList() ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Enrolments()
    {
        var result = await api.GetAsync<List<RegistrationViewModel>>("api/registrations");
        ViewBag.ApiError = result.Error;
        return View(result.Value ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Results()
    {
        var events = await api.GetAsync<List<EventViewModel>>("api/events/mine");
        var registrations = await api.GetAsync<List<RegistrationViewModel>>("api/registrations");
        var results = await api.GetAsync<List<ResultViewModel>>("api/results");
        var registrationIds = (registrations.Value ?? []).Select(r => r.RegistrationId).ToHashSet();
        ViewBag.ApiError = events.Error ?? registrations.Error ?? results.Error;
        ViewBag.Registrations = registrations.Value ?? [];
        return View((results.Value ?? []).Where(r => registrationIds.Contains(r.RegistrationId)).ToList());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel());

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await api.GetAsync<List<EventViewModel>>("api/events/mine");
        var item = result.Value?.FirstOrDefault(e => e.EventId == id);
        if (!result.Succeeded || item is null)
        {
            TempData["Notice"] = result.Error ?? "Event unavailable.";
            return RedirectToAction(nameof(Index));
        }
        var e = item;
        return View("EventForm", new EventFormViewModel { EventId = e.EventId, Name = e.Name, Description = e.Description, Date = e.Date, Location = e.Location, Distance = e.Distance, EventType = e.EventType, BannerImageUrl = e.BannerImageUrl });
    }

    [HttpPost]
    public async Task<IActionResult> SaveEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("EventForm", model);
        var payload = new { model.Name, model.Description, model.Date, model.Location, Distance = model.Distance, model.EventType };
        var result = model.EventId == 0
            ? await api.PostAsync<object, EventViewModel>("api/events", payload)
            : await api.PutAsync<object, EventViewModel>($"api/events/{model.EventId}", payload);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error ?? "Unable to save the event.");
            return View("EventForm", model);
        }
        var eventId = result.Value?.EventId ?? model.EventId;
        if (model.BannerFile is { Length: > 0 })
        {
            var banner = await api.UploadFileAsync<object>($"api/events/{eventId}/banner", model.BannerFile);
            if (!banner.Succeeded)
            {
                model.EventId = eventId;
                ModelState.AddModelError("", banner.Error ?? "The event was saved, but its banner could not be uploaded.");
                return View("EventForm", model);
            }
        }
        TempData["Notice"] = "Event saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await api.DeleteAsync<object>($"api/events/{id}");
        TempData["Notice"] = result.Succeeded ? "Event deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CategoryFormViewModel model)
    {
        if (model.MinimumAge > model.MaximumAge)
            ModelState.AddModelError(nameof(model.MaximumAge), "Maximum age must be at least the minimum age.");
        if (!ModelState.IsValid)
        {
            TempData["Notice"] = "Please enter valid category details.";
            return RedirectToAction(nameof(Index));
        }
        var payload = new { model.Name, model.MinimumAge, model.MaximumAge, model.Distance, model.Price, model.Capacity, model.EventId };
        var result = model.CategoryId == 0
            ? await api.PostAsync<object, object>("api/categories", payload)
            : await api.PutAsync<object, object>($"api/categories/{model.CategoryId}", payload);
        TempData["Notice"] = result.Succeeded ? "Category saved." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await api.DeleteAsync<object>($"api/categories/{id}");
        TempData["Notice"] = result.Succeeded ? "Category deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateWeatherAlert(WeatherAlertFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Notice"] = "Please enter a valid weather alert.";
            return RedirectToAction(nameof(Index));
        }
        var result = await api.PostAsync<WeatherAlertFormViewModel, object>("api/weather/alerts", model);
        TempData["Notice"] = result.Succeeded ? "Weather alert published." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRegistration(StatusFormViewModel model)
    {
        var result = await api.PutAsync<object, object>($"api/enrolments/{model.RegistrationId}/status", new { model.Status });
        TempData["Notice"] = result.Succeeded ? "Registration status updated." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddResult(ResultFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Notice"] = "Enter a valid finish time, position, and registration.";
            return RedirectToAction(nameof(Index));
        }
        var result = await api.PostAsync<object, object>("api/results", new { model.RegistrationId, model.FinishTime, model.Position, model.Status });
        TempData["Notice"] = result.Succeeded ? "Result recorded." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditResult(int id)
    {
        var result = await api.GetAsync<List<ResultViewModel>>("api/results");
        var item = result.Value?.FirstOrDefault(r => r.ResultId == id);
        if (!result.Succeeded || item is null)
        {
            TempData["Notice"] = result.Error ?? "Result unavailable.";
            return RedirectToAction(nameof(Results));
        }
        var ownedRegistrations = await api.GetAsync<List<RegistrationViewModel>>("api/registrations");
        var registration = ownedRegistrations.Value?.FirstOrDefault(r => r.RegistrationId == item.RegistrationId);
        if (registration is null)
            return RedirectToAction(nameof(Results));
        return View("ResultForm", new ResultFormViewModel
        {
            ResultId = item.ResultId,
            RegistrationId = item.RegistrationId,
            FinishTime = item.FinishTime,
            Position = item.Position,
            Status = item.Status
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(ResultFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Notice"] = "Enter a valid finish time, position, and result status.";
            return RedirectToAction(nameof(Results));
        }
        var payload = new { model.FinishTime, model.Position, model.Status };
        var result = model.ResultId == 0
            ? await api.PostAsync<object, object>("api/results", new { model.RegistrationId, model.FinishTime, model.Position, model.Status })
            : await api.PutAsync<object, object>($"api/results/{model.ResultId}", payload);
        TempData["Notice"] = result.Succeeded ? "Result saved." : result.Error;
        return RedirectToAction(nameof(Results));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteResult(int id)
    {
        var result = await api.DeleteAsync<object>($"api/results/{id}");
        TempData["Notice"] = result.Succeeded ? "Result deleted." : result.Error;
        return RedirectToAction(nameof(Results));
    }

    private async Task<(ApiResult<List<EventViewModel>> Events, ApiResult<List<CategoryViewModel>> Categories)> GetMyEventsAndCategories()
    {
        var eventsTask = api.GetAsync<List<EventViewModel>>("api/events/mine");
        var categoriesTask = api.GetAsync<List<CategoryViewModel>>("api/categories");
        await Task.WhenAll(eventsTask, categoriesTask);
        return (eventsTask.Result, categoriesTask.Result);
    }
}
