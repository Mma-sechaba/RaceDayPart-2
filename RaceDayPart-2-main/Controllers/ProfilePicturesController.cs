using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/users/me/profile-picture"), Authorize]
public sealed class ProfilePicturesController(RaceDayDbContext db, IBlobStorageService blobStorage) : ControllerBase
{
    [HttpPost, Consumes("multipart/form-data"), RequestSizeLimit(ImageValidator.MaximumBytes + 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        var error = await ImageValidator.ValidateAsync(file, cancellationToken);
        if (error is not null) return BadRequest(new { message = error });

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var participantRole = User.IsInRole("Participant");
        var organiserRole = User.IsInRole("Organiser");
        if (!participantRole && !organiserRole) return Forbid();
        var participant = participantRole
            ? await db.Participants.FirstOrDefaultAsync(x => x.ParticipantId == id, cancellationToken)
            : null;
        var organiser = organiserRole
            ? await db.Organisers.FirstOrDefaultAsync(x => x.OrganiserId == id, cancellationToken)
            : null;
        if (participantRole && participant is null || organiserRole && organiser is null) return NotFound();

        try
        {
            await using var stream = file!.OpenReadStream();
            var url = await blobStorage.UploadAsync(stream, file.FileName, file.ContentType, "profile-pictures", cancellationToken);
            if (participant is not null) participant.ProfilePictureUrl = url;
            if (organiser is not null) organiser.ProfilePictureUrl = url;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { profilePictureUrl = url });
        }
        catch (Exception ex) when (ex is Azure.RequestFailedException or InvalidOperationException)
        {
            return StatusCode(503, new { message = "Image storage is unavailable." });
        }
    }
}
