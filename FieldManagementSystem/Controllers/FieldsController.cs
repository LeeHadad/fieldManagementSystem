using FieldManagementSystem.DTOs;
using FieldManagementSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FieldManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FieldsController(IFieldService fieldService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FieldDto>>> GetMyFields([FromHeader(Name = "X-User-Email")] string email)
    {
        var fields = await fieldService.GetFieldsByEmailAsync(email);
        return Ok(fields);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FieldDto>> GetFieldById(
        [FromHeader(Name = "X-User-Email")] string email,
        int id)
    {
        var field = await fieldService.GetFieldByIdAsync(email, id);
        if (field == null)
            return NotFound(new { error = "Field not found." });

        return Ok(field);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateField(
        [FromHeader(Name = "X-User-Email")] string email,
        [FromBody] CreateFieldRequest request)
    {
        var created = await fieldService.CreateFieldAsync(email, request.Name);
        return CreatedAtAction(nameof(GetFieldById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateField(
        [FromHeader(Name = "X-User-Email")] string email,
        int id,
        [FromBody] UpdateFieldRequest request)
    {
        var ok = await fieldService.UpdateFieldAsync(email, id, request.Name);
        if (!ok) return NotFound(new { error = "Field not found." });

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteField([FromHeader(Name = "X-User-Email")] string email, int id)
    {
        var ok = await fieldService.DeleteFieldAsync(email, id);
        if (!ok) return NotFound(new { error = "Field not found or you don't have permission." });

        return NoContent();
    }
}
