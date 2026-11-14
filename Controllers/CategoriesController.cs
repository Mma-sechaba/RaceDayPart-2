using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/categories")]
public class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet, AllowAnonymous] public async Task<IActionResult> GetAll() => Ok(await db.Categories.AsNoTracking().Include(x => x.Event).Select(x => new { x.CategoryId, x.EventId, name = x.CategoryName, x.MinimumAge, x.MaximumAge, x.Distance, x.Price, x.Capacity, enrolledCount = x.Registrations.Count(r => r.Status.ToLower() != "cancelled"), eventName = x.Event!.EventName }).ToListAsync());
    [HttpGet("{id:int}"), AllowAnonymous] public async Task<IActionResult> Get(int id) { var c = await db.Categories.AsNoTracking().Where(x => x.CategoryId == id).Select(x => new { x.CategoryId, x.EventId, x.CategoryName, x.MinimumAge, x.MaximumAge, x.Distance, x.Price, x.Capacity, enrolledCount = x.Registrations.Count(r => r.Status.ToLower() != "cancelled") }).FirstOrDefaultAsync(); return c is null ? NotFound() : Ok(c); }
    [HttpPost, Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(CategoryRequest request)
    {
        if (request.MinimumAge > request.MaximumAge || string.IsNullOrWhiteSpace(request.Name) || request.Distance <= 0 || request.Distance > 9999.99m || decimal.Round(request.Distance, 2) != request.Distance || request.Price < 0 || decimal.Round(request.Price, 2) != request.Price) return BadRequest(new { message = "Category name, age range, distance, and price are invalid." });
        if (!request.EventId.HasValue) return BadRequest(new { message = "EventId is required." });
        var ev = await db.Events.FindAsync(request.EventId.Value); if (ev is null) return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid();
        var c = new Category { EventId = request.EventId.Value, CategoryName = request.Name.Trim(), MinimumAge = request.MinimumAge, MaximumAge = request.MaximumAge, Distance = request.Distance, Price = request.Price, Capacity = request.Capacity };
        db.Categories.Add(c); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = c.CategoryId }, CategoryView(c));
    }
    [HttpPut("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var c = await db.Categories.Include(x => x.Event).Include(x => x.Registrations).FirstOrDefaultAsync(x => x.CategoryId == id); if (c is null) return NotFound();
        if (c.Event!.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid();
        if (request.MinimumAge > request.MaximumAge || string.IsNullOrWhiteSpace(request.Name) || request.Distance <= 0 || request.Distance > 9999.99m || decimal.Round(request.Distance, 2) != request.Distance || request.Price < 0 || decimal.Round(request.Price, 2) != request.Price) return BadRequest(new { message = "Category name, age range, distance, and price are invalid." });
        if (request.Capacity.HasValue && request.Capacity.Value < await db.Registrations.CountAsync(r => r.CategoryId == id && r.Status.ToLower() != "cancelled")) return BadRequest(new { message = "Capacity cannot be lower than the current enrolment count." });
        c.CategoryName = request.Name.Trim(); c.MinimumAge = request.MinimumAge; c.MaximumAge = request.MaximumAge; c.Distance = request.Distance; c.Price = request.Price; c.Capacity = request.Capacity;
        await db.SaveChangesAsync(); return Ok(CategoryView(c));
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id) { var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryId == id); if (c is null) return NotFound(); if (c.Event!.OrganiserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid(); db.Categories.Remove(c); try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(new { message = "Category cannot be deleted while registrations use it." }); } return NoContent(); }

    private static object CategoryView(Category c) => new { c.CategoryId, c.EventId, name = c.CategoryName, c.MinimumAge, c.MaximumAge, c.Distance, c.Price, c.Capacity, enrolledCount = c.Registrations.Count(r => !r.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) };
}
