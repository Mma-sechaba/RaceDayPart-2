using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using System.Globalization;

namespace RaceDay.Web.Controllers;

public sealed class HomeController(RaceDayApiClient api) : Controller
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
        var result = await api.GetAsync<List<EventViewModel>>("api/events", query);
        ViewBag.ApiError = result.Error;
        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.Location = location;
        ViewBag.From = from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ViewBag.To = to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ViewBag.UpcomingOnly = upcomingOnly;
        return View(result.Value ?? []);
    }

    public async Task<IActionResult> Details(int id)
    {
        var eventsTask = api.GetAsync<List<EventViewModel>>("api/events");
        var categoriesTask = api.GetAsync<List<CategoryViewModel>>("api/categories");
        await Task.WhenAll(eventsTask, categoriesTask);
        var result = eventsTask.Result;
        var item = result.Value?.FirstOrDefault(e => e.EventId == id);
        if (!result.Succeeded || item is null)
        {
            TempData["Notice"] = result.Error ?? "Event unavailable.";
            return RedirectToAction(nameof(Index));
        }
        item.Categories = (categoriesTask.Result.Value ?? []).Where(c => c.EventId == id).ToList();
        return View(item);
    }

    public IActionResult Error() => View();
}
