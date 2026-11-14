using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/results")]
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetAll() => Ok(await db.Results.AsNoTracking().Include(x => x.Registration).ThenInclude(x => x!.Participant).Include(x => x.Registration).ThenInclude(x => x!.Event).Include(x => x.Registration).ThenInclude(x => x!.Category).OrderBy(x => x.Position).ThenBy(x => x.FinishTime).Select(x => new { x.ResultId, x.FinishTime, x.Position, x.Status, x.RegistrationId, participant = x.Registration!.Participant!.FirstName + " " + x.Registration.Participant.LastName, eventName = x.Registration.Event!.EventName, eventDate = x.Registration.Event.EventDate, category = x.Registration.Category!.CategoryName, totalFinishers = db.Results.Count(other => other.Status == "Finished" && other.Registration!.EventId == x.Registration.EventId && other.Registration.CategoryId == x.Registration.CategoryId) }).ToListAsync());

    [HttpGet("my"), HttpGet("me"), Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyResults() { var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); return Ok(await db.Results.AsNoTracking().Include(x => x.Registration).ThenInclude(x => x!.Event).Include(x => x.Registration).ThenInclude(x => x!.Category).Where(x => x.Registration!.ParticipantId == id).Select(x => new { x.ResultId, x.FinishTime, x.Position, x.Status, eventName = x.Registration!.Event!.EventName, eventDate = x.Registration.Event.EventDate, category = x.Registration.Category!.CategoryName, totalFinishers = db.Results.Count(other => other.Status == "Finished" && other.Registration!.EventId == x.Registration.EventId && other.Registration.CategoryId == x.Registration.CategoryId), positionOfNFinishers = x.Status == "Finished" ? x.Position + " of " + db.Results.Count(other => other.Status == "Finished" && other.Registration!.EventId == x.Registration.EventId && other.Registration.CategoryId == x.Registration.CategoryId) + " finishers" : x.Status }).ToListAsync()); }

    [HttpGet("{id:int}"), Authorize]
    public async Task<IActionResult> Get(int id) { var r = await db.Results.AsNoTracking().Include(x => x.Registration).ThenInclude(x => x!.Event).Include(x => x.Registration).ThenInclude(x => x!.Participant).Include(x => x.Registration).ThenInclude(x => x!.Category).FirstOrDefaultAsync(x => x.ResultId == id); if (r is null) return NotFound(); var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); if (User.IsInRole("Participant") && r.Registration!.ParticipantId != userId) return Forbid(); if (User.IsInRole("Organiser") && r.Registration!.Event!.OrganiserId != userId) return Forbid(); return Ok(new { r.ResultId, r.RegistrationId, r.FinishTime, r.Position, r.Status, participantName = r.Registration!.Participant!.FirstName + " " + r.Registration.Participant.LastName, eventName = r.Registration.Event!.EventName, category = r.Registration.Category!.CategoryName }); }

    [HttpPost, Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(ResultRequest request) { var registration = await db.Registrations.Include(x => x.Event).FirstOrDefaultAsync(x => x.RegistrationId == request.RegistrationId); if (registration is null) return NotFound(new { message = "Registration does not exist." }); if (registration.Event!.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid(); if (await db.Results.AnyAsync(x => x.RegistrationId == request.RegistrationId)) return Conflict(new { message = "A result already exists for this registration." }); if (request.FinishTime is not TimeSpan finishTime || !Valid(finishTime, request.Position, request.Status)) return BadRequest(new { message = "Result requires a non-negative time under 24 hours, positive position, and status Finished, DNF, or DQ." }); var result = new Result { RegistrationId = request.RegistrationId, FinishTime = finishTime, Position = request.Position, Status = CanonicalStatus(request.Status) }; db.Results.Add(result); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = result.ResultId }, new { result.ResultId, result.RegistrationId, result.FinishTime, result.Position, result.Status }); }

    [HttpPut("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, ResultUpdateRequest request) { var r = await db.Results.Include(x => x.Registration).ThenInclude(x => x!.Event).FirstOrDefaultAsync(x => x.ResultId == id); if (r is null) return NotFound(); if (r.Registration!.Event!.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid(); if (request.FinishTime is not TimeSpan finishTime || !Valid(finishTime, request.Position, request.Status)) return BadRequest(new { message = "Result requires a non-negative time under 24 hours, positive position, and status Finished, DNF, or DQ." }); r.FinishTime = finishTime; r.Position = request.Position; r.Status = CanonicalStatus(request.Status); await db.SaveChangesAsync(); return Ok(new { r.ResultId, r.RegistrationId, r.FinishTime, r.Position, r.Status }); }

    [HttpDelete("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id) { var r = await db.Results.Include(x => x.Registration).ThenInclude(x => x!.Event).FirstOrDefaultAsync(x => x.ResultId == id); if (r is null) return NotFound(); if (r.Registration!.Event!.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid(); db.Results.Remove(r); await db.SaveChangesAsync(); return NoContent(); }

    private static bool Valid(TimeSpan finishTime, int position, string? status) =>
        finishTime >= TimeSpan.Zero && finishTime < TimeSpan.FromDays(1) && position >= 1 &&
        new[] { "Finished", "DNF", "DQ" }.Contains(status?.Trim(), StringComparer.OrdinalIgnoreCase);

    private static string CanonicalStatus(string status) => status.Trim().ToUpperInvariant() switch { "DNF" => "DNF", "DQ" => "DQ", _ => "Finished" };
}
