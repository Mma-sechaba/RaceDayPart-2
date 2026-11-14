using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/registrations")]
public class RegistrationsController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet, Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetAll() => Ok(await db.Registrations.AsNoTracking().Where(x => x.Event!.OrganiserId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)).Select(x => new
    {
        x.RegistrationId,
        x.ParticipantId,
        x.EventId,
        x.CategoryId,
        x.RegistrationDate,
        x.Status,
        participantName = x.Participant!.FirstName + " " + x.Participant.LastName,
        email = x.Participant.Email,
        eventName = x.Event!.EventName,
        eventDate = x.Event.EventDate,
        categoryName = x.Category!.CategoryName,
        participant = new { x.Participant.FirstName, x.Participant.LastName, x.Participant.Email, x.Participant.ProfilePictureUrl },
        @event = new { x.Event.EventName, x.Event.EventDate, x.Event.Location, x.Event.DistanceKm },
        category = new { x.Category.CategoryName, x.Category.Price, x.Category.Distance },
        results = x.Results.Select(result => new { result.ResultId, result.FinishTime, result.Position, result.Status })
    }).ToListAsync());

    [HttpGet("me"), Authorize(Roles = "Participant")]
    public async Task<IActionResult> Mine()
    {
        var participantId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await db.Registrations.AsNoTracking().Where(x => x.ParticipantId == participantId).Select(x => new
        {
            x.RegistrationId,
            x.ParticipantId,
            x.EventId,
            x.CategoryId,
            x.RegistrationDate,
            x.Status,
            participantName = x.Participant!.FirstName + " " + x.Participant.LastName,
            email = x.Participant.Email,
            eventName = x.Event!.EventName,
            eventDate = x.Event.EventDate,
            categoryName = x.Category!.CategoryName,
            participant = new { x.Participant.FirstName, x.Participant.LastName, x.Participant.Email, x.Participant.ProfilePictureUrl },
            @event = new { x.Event.EventName, x.Event.EventDate, x.Event.Location, x.Event.DistanceKm },
            category = new { x.Category.CategoryName, x.Category.Price, x.Category.Distance },
            results = x.Results.Select(result => new { result.ResultId, result.FinishTime, result.Position, result.Status })
        }).ToListAsync());
    }

    [HttpGet("{id:int}"), Authorize]
    public async Task<IActionResult> Get(int id) { var r = await db.Registrations.AsNoTracking().Include(x => x.Event).FirstOrDefaultAsync(x => x.RegistrationId == id); if (r is null) return NotFound(); if (!CanAccess(r)) return Forbid(); return Ok(await RegistrationView(id)); }

    [HttpPost, Authorize(Roles = "Participant")]
    public async Task<IActionResult> Create(RegistrationRequest request)
    {
        var participantId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ev = await db.Events.FindAsync(request.EventId); if (ev is null) return NotFound(new { message = "Event does not exist." });
        if (ev.EventDate.Date < DateTime.Today) return BadRequest(new { message = "Past events cannot be joined." });
        var category = await db.Categories.FindAsync(request.CategoryId); if (category is null || category.EventId != request.EventId) return NotFound(new { message = "Category does not exist for the selected event." });
        var existing = await db.Registrations.FirstOrDefaultAsync(x => x.ParticipantId == participantId && x.EventId == request.EventId);
        if (existing is not null && !existing.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) return Conflict(new { message = "Participant is already registered for this event." });
        if (existing is not null && await db.Results.AnyAsync(x => x.RegistrationId == existing.RegistrationId)) return Conflict(new { message = "An enrolment with a recorded result cannot be reactivated." });
        if (category.Capacity.HasValue && await db.Registrations.CountAsync(x => x.CategoryId == category.CategoryId && x.Status.ToLower() != "cancelled" && (existing == null || x.RegistrationId != existing.RegistrationId)) >= category.Capacity.Value)
            return Conflict(new { message = "The selected category has reached capacity." });
        var participant = await db.Participants.FindAsync(participantId);
        if (participant is null) return NotFound(new { message = "Participant does not exist." });
        var age = DateTime.Today.Year - participant.DateOfBirth.Year;
        if (participant.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
        if (age < category.MinimumAge || age > category.MaximumAge) return BadRequest(new { message = "Participant age is outside the category age range." });
        var r = existing ?? new Registration { ParticipantId = participantId, EventId = request.EventId };
        r.CategoryId = request.CategoryId; r.Status = "Pending"; r.RegistrationDate = DateTime.Now;
        if (existing is null) db.Registrations.Add(r);
        await db.SaveChangesAsync(); return StatusCode(201, await RegistrationView(r.RegistrationId));
    }

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Update(int id, RegistrationUpdateRequest request)
    {
        var r = await db.Registrations.Include(x => x.Event).FirstOrDefaultAsync(x => x.RegistrationId == id); if (r is null) return NotFound();
        if (!CanAccess(r)) return Forbid();
        if (request.CategoryId.HasValue) { var category = await db.Categories.FindAsync(request.CategoryId.Value); if (category is null || category.EventId != r.EventId) return BadRequest(new { message = "Category must belong to the registered event." }); if (!r.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) && category.Capacity.HasValue && await db.Registrations.CountAsync(x => x.CategoryId == category.CategoryId && x.Status.ToLower() != "cancelled" && x.RegistrationId != id) >= category.Capacity.Value) return Conflict(new { message = "The selected category has reached capacity." }); r.CategoryId = request.CategoryId.Value; }
        if (request.Status is not null) { if (!User.IsInRole("Organiser")) return Forbid(); if (r.Event!.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid(); if (!new[] { "Pending", "Confirmed", "Cancelled" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Status must be Pending, Confirmed, or Cancelled." }); if (!request.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) && r.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) { var category = await db.Categories.FindAsync(r.CategoryId); if (category?.Capacity is int capacity && await db.Registrations.CountAsync(x => x.CategoryId == r.CategoryId && x.Status.ToLower() != "cancelled" && x.RegistrationId != id) >= capacity) return Conflict(new { message = "The selected category has reached capacity." }); } r.Status = request.Status.Trim().ToLowerInvariant() switch { "pending" => "Pending", "confirmed" => "Confirmed", _ => "Cancelled" }; }
        await db.SaveChangesAsync(); return Ok(await RegistrationView(r.RegistrationId));
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Delete(int id) { var r = await db.Registrations.Include(x => x.Event).FirstOrDefaultAsync(x => x.RegistrationId == id); if (r is null) return NotFound(); if (!CanAccess(r)) return Forbid(); if (User.IsInRole("Participant") && await db.Results.AnyAsync(x => x.RegistrationId == id)) return Conflict(new { message = "An enrolment with a recorded result cannot be cancelled by its participant." }); r.Status = "Cancelled"; await db.SaveChangesAsync(); return NoContent(); }

    [HttpPost("/api/enrolments"), Authorize(Roles = "Participant")]
    public Task<IActionResult> Enrol([FromBody] RegistrationRequest request) => Create(request);
    [HttpDelete("/api/enrolments/{id:int}"), Authorize]
    public Task<IActionResult> CancelEnrolment(int id) => Delete(id);
    [HttpPut("/api/enrolments/{id:int}/status"), Authorize(Roles = "Organiser")]
    public Task<IActionResult> UpdateEnrolmentStatus(int id, [FromBody] RegistrationUpdateRequest request) => Update(id, new RegistrationUpdateRequest { Status = request.Status });

    private bool CanAccess(Registration r)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return User.IsInRole("Organiser") ? r.Event?.OrganiserId == id : User.IsInRole("Participant") && r.ParticipantId == id;
    }

    private async Task<object?> RegistrationView(int id)
    {
        return await db.Registrations.AsNoTracking().Where(r => r.RegistrationId == id)
            .Select(r => new
            {
                r.RegistrationId,
                r.ParticipantId,
                r.EventId,
                r.CategoryId,
                r.RegistrationDate,
                r.Status,
                participant = new { r.Participant!.FirstName, r.Participant.LastName, r.Participant.Email, r.Participant.ProfilePictureUrl },
                participantName = r.Participant.FirstName + " " + r.Participant.LastName,
                email = r.Participant.Email,
                @event = new { r.Event!.EventName, r.Event.EventDate, r.Event.Location, r.Event.DistanceKm },
                eventName = r.Event.EventName,
                eventDate = r.Event.EventDate,
                category = new { r.Category!.CategoryName, r.Category.Price, r.Category.Distance },
                categoryName = r.Category.CategoryName,
                results = r.Results.Select(result => new { result.ResultId, result.FinishTime, result.Position, result.Status })
            }).FirstOrDefaultAsync();
    }
}
