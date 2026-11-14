using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/events")]
public class EventsController(RaceDayDbContext db, IBlobStorageService blobStorage) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? type, [FromQuery] string? location, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] bool upcomingOnly = false)
    {
        var q = db.Events.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.EventName.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        if (!string.IsNullOrWhiteSpace(type))
        {
            type = type.Trim().ToLowerInvariant() switch
            {
                "run" => "Run",
                "walk" => "Walk",
                "cycle" => "Cycle",
                _ => type.Trim()
            };
            q = q.Where(x => x.EventType == type);
        }
        if (!string.IsNullOrWhiteSpace(location)) q = q.Where(x => x.Location.Contains(location));
        if (from.HasValue) q = q.Where(x => x.EventDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(x => x.EventDate <= to.Value.Date);
        if (upcomingOnly) q = q.Where(x => x.EventDate >= DateTime.Today);
        return Ok(await q.OrderBy(x => x.EventDate).Select(x => new { x.EventId, name = x.EventName, description = x.Description, date = x.EventDate, x.Location, x.DistanceKm, x.EventType, x.Status, x.OrganiserId, x.BannerImageUrl }).ToListAsync());
    }

    [HttpGet("mine"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Mine() => Ok(await db.Events.AsNoTracking().Where(x => x.OrganiserId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)).OrderBy(x => x.EventDate).ToListAsync());

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<IActionResult> Get(int id) { var e = await db.Events.AsNoTracking().Include(x => x.Categories).Include(x => x.WeatherRecords).FirstOrDefaultAsync(x => x.EventId == id); return e is null ? NotFound() : Ok(EventView(e)); }

    [HttpPost, Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (!new[] { "Run", "Walk", "Cycle" }.Contains(request.EventType.Trim(), StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "EventType must be Run, Walk, or Cycle." });
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location) || request.Date == default || request.EffectiveDistanceKm <= 0 || request.EffectiveDistanceKm > 9999.99m || decimal.Round(request.EffectiveDistanceKm, 2) != request.EffectiveDistanceKm) return BadRequest(new { message = "Event name, location, date, and a positive distance with at most two decimal places are required." });
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var e = new RaceEvent { OrganiserId = id, EventName = request.Name.Trim(), Description = request.Description, EventDate = request.Date.Date, Location = request.Location.Trim(), DistanceKm = request.EffectiveDistanceKm, EventType = CanonicalType(request.EventType), Status = "Available" };
        db.Events.Add(e); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = e.EventId }, EventView(e));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var e = await db.Events.FindAsync(id); if (e is null) return NotFound();
        if (e.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid();
        if (!new[] { "Run", "Walk", "Cycle" }.Contains(request.EventType.Trim(), StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "EventType must be Run, Walk, or Cycle." });
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location) || request.Date == default || request.EffectiveDistanceKm <= 0 || request.EffectiveDistanceKm > 9999.99m || decimal.Round(request.EffectiveDistanceKm, 2) != request.EffectiveDistanceKm) return BadRequest(new { message = "Event name, location, date, and a positive distance with at most two decimal places are required." });
        e.EventName = request.Name.Trim(); e.Description = request.Description; e.EventDate = request.Date.Date; e.Location = request.Location.Trim(); e.DistanceKm = request.EffectiveDistanceKm; e.EventType = CanonicalType(request.EventType);
        await db.SaveChangesAsync(); return Ok(EventView(e));
    }

    [HttpPost("{id:int}/banner"), Authorize(Roles = "Organiser"), Consumes("multipart/form-data"), RequestSizeLimit(ImageValidator.MaximumBytes + 1024)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        var e = await db.Events.FindAsync(id); if (e is null) return NotFound();
        if (e.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid();
        var error = await ImageValidator.ValidateAsync(file, cancellationToken); if (error is not null) return BadRequest(new { message = error });
        try
        {
            await using var stream = file!.OpenReadStream();
            e.BannerImageUrl = await blobStorage.UploadAsync(stream, file.FileName, file.ContentType, "event-banners", cancellationToken);
            await db.SaveChangesAsync();
            return Ok(new { e.EventId, e.BannerImageUrl });
        }
        catch (Exception ex) when (ex is Azure.RequestFailedException or InvalidOperationException)
        {
            return StatusCode(503, new { message = "Image storage is unavailable." });
        }
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id) { var e = await db.Events.Include(x => x.Registrations).Include(x => x.Categories).Include(x => x.WeatherRecords).FirstOrDefaultAsync(x => x.EventId == id); if (e is null) return NotFound(); if (e.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid(); if (e.Registrations.Count != 0) return Conflict(new { message = "Event cannot be deleted while it has enrolments." }); db.Categories.RemoveRange(e.Categories); db.WeatherRecords.RemoveRange(e.WeatherRecords); db.Events.Remove(e); try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(new { message = "Event cannot be deleted while related records exist." }); } return NoContent(); }

    [HttpGet("{eventId:int}/enrolments"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound();
        if (e.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid();
        return Ok(await db.Registrations.Where(r => r.EventId == eventId).Include(r => r.Participant).Include(r => r.Category).Include(r => r.Results).Select(r => new { r.RegistrationId, r.Status, r.RegistrationDate, participant = r.Participant!.FirstName + " " + r.Participant.LastName, r.Participant!.Email, category = r.Category!.CategoryName, results = r.Results.Select(result => new { result.ResultId, result.FinishTime, result.Position, result.Status }) }).ToListAsync());
    }

    private static string CanonicalType(string value) => value.Trim().ToLowerInvariant() switch { "run" => "Run", "walk" => "Walk", _ => "Cycle" };

    private static object EventView(RaceEvent e) => new
    {
        e.EventId,
        e.OrganiserId,
        name = e.EventName,
        description = e.Description,
        date = e.EventDate,
        e.Location,
        e.DistanceKm,
        e.EventType,
        e.Status,
        e.BannerImageUrl,
        categories = e.Categories.Select(c => new { c.CategoryId, c.EventId, name = c.CategoryName, c.MinimumAge, c.MaximumAge, c.Distance, c.Price, c.Capacity }),
        weatherRecords = e.WeatherRecords.Select(w => new { w.WeatherId, w.Temperature, w.WeatherCondition, w.WindSpeed, w.WeatherDate })
    };
}
