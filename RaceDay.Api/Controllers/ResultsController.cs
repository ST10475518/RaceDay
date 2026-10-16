using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Race results: Organisers capture them; Participants view their own.</summary>
[ApiController]
[Authorize]
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    private IQueryable<Result> Query() => db.Results
        .Include(r => r.Enrolment!).ThenInclude(e => e.Event)
        .Include(r => r.Enrolment!).ThenInclude(e => e.Category)
        .Include(r => r.Enrolment!).ThenInclude(e => e.Participant);

   
    [HttpPost("api/results")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Create(ResultRequest req)
    {
        var enrolment = await db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.Id == req.EnrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event!.OrganiserId != User.GetUserId()) return Forbid();
        if (await db.Results.AnyAsync(r => r.EnrolmentId == req.EnrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment." });

        var r = new Result { EnrolmentId = req.EnrolmentId, FinishTime = req.FinishTime, Position = req.Position };
        db.Results.Add(r);
        await db.SaveChangesAsync();
        var saved = await Query().AsNoTracking().FirstAsync(x => x.Id == r.Id);
        return CreatedAtAction(nameof(Mine), null, saved.ToDto());
    }

    
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Update(int id, UpdateResultRequest req)
    {
        var r = await Query().FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        if (r.Enrolment!.Event!.OrganiserId != User.GetUserId()) return Forbid();
        r.FinishTime = req.FinishTime; r.Position = req.Position;
        await db.SaveChangesAsync();
        return Ok(r.ToDto());
    }

   
    [HttpGet("api/events/{eventId:int}/results")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultDto>>> ForEvent(int eventId)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();
        var list = await Query().AsNoTracking().Where(r => r.Enrolment!.EventId == eventId)
                                .OrderBy(r => r.Position).ToListAsync();
        return Ok(list.Select(r => r.ToDto()).ToList());
    }

    
    [HttpGet("api/results/my")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultDto>>> Mine()
    {
        var me = User.GetUserId();
        var list = await Query().AsNoTracking().Where(r => r.Enrolment!.ParticipantId == me)
                                .OrderByDescending(r => r.RecordedAt).ToListAsync();
        return Ok(list.Select(r => r.ToDto()).ToList());
    }
}
