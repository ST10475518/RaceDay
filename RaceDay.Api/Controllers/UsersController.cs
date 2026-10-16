using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;


[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(RaceDayDbContext db) : ControllerBase
{
   
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await db.Users.FindAsync(User.GetUserId());
        return user is null ? Unauthorized() : Ok(user.ToDto());
    }

    
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest req)
    {
        var user = await db.Users.FindAsync(User.GetUserId());
        if (user is null) return Unauthorized();
        user.FirstName = req.FirstName.Trim();
        user.LastName = req.LastName.Trim();
        user.PhoneNumber = req.PhoneNumber;
        user.DateOfBirth = req.DateOfBirth;
        await db.SaveChangesAsync();
        return Ok(user.ToDto());
    }
}
