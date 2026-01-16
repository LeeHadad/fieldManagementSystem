using FieldManagementSystem.DTOs;
using FieldManagementSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FieldManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var created = await userService.CreateUserAsync(request.Email);
        return CreatedAtAction(nameof(GetMe), null, created);
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe([FromHeader(Name = "X-User-Email")] string email)
    {
        var user = await userService.GetUserByEmailAsync(email);
        if (user == null)
            return NotFound(new { error = "User not found. Create via POST /api/users." });

        return Ok(user);
    }
}