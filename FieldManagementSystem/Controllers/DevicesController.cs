using FieldManagementSystem.DTOs;
using FieldManagementSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FieldManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController(IDeviceService deviceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DeviceDto>>> GetMyDevices([FromHeader(Name = "X-User-Email")] string email)
    {
        var devices = await deviceService.GetDevicesByEmailAsync(email);
        return Ok(devices);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> GetDeviceById([FromHeader(Name = "X-User-Email")] string email, int id)
    {
        var device = await deviceService.GetDeviceByIdAsync(email, id);
        if (device == null)
            return NotFound(new { error = "Device not found." });

        return Ok(device);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDevice(
        [FromHeader(Name = "X-User-Email")] string email,
        [FromBody] CreateDeviceRequest request)
    {
        var created = await deviceService.CreateDeviceAsync(email, request.Name);
        return CreatedAtAction(nameof(GetDeviceById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDevice(
        [FromHeader(Name = "X-User-Email")] string email,
        int id,
        [FromBody] UpdateDeviceRequest request)
    {
        var ok = await deviceService.UpdateDeviceAsync(email, id, request.Name);
        if (!ok) return NotFound(new { error = "Device not found." });

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDevice([FromHeader(Name = "X-User-Email")] string email, int id)
    {
        var ok = await deviceService.DeleteDeviceAsync(email, id);
        if (!ok) return NotFound(new { error = "Device not found or you don't have permission." });

        return NoContent();
    }
}
