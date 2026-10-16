using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryDto>>> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        var list = await db.Categories.AsNoTracking().Where(c => c.EventId == eventId).OrderBy(c => c.Name).ToListAsync();
        return Ok(list.Select(c => c.ToDto()).ToList());
    }

   
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, CategoryRequest req)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();
        var c = new Category { EventId = eventId, Name = req.Name, Description = req.Description };
        db.Categories.Add(c);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetForEvent), new { eventId }, c.ToDto());
    }

    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest req)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        if (c.Event!.OrganiserId != User.GetUserId()) return Forbid();
        c.Name = req.Name; c.Description = req.Description;
        await db.SaveChangesAsync();
        return Ok(c.ToDto());
    }

    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        if (c.Event!.OrganiserId != User.GetUserId()) return Forbid();
        if (await db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Conflict(new { message = "Category has enrolments and cannot be deleted." });
        db.Categories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
