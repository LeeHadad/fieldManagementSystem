using FieldManagementSystem.Data;
using FieldManagementSystem.DTOs;
using FieldManagementSystem.Interfaces;
using FieldManagementSystem.Models;
using FieldManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;

namespace FieldManagementSystem.Services;

public class DeviceService(AppDbContext context, ILogger<DeviceService> logger) : IDeviceService
{
    public async Task<List<DeviceDto>> GetDevicesByEmailAsync(string email)
    {
        email = InputGuards.NormalizeEmail(email);

        return await context.Controllers
            .AsNoTracking()
            .Where(c => c.User.Email == email)
            .Select(c => new DeviceDto(c.Id, c.Name))
            .ToListAsync();
    }

    public async Task<DeviceDto?> GetDeviceByIdAsync(string email, int deviceId)
    {
        email = InputGuards.NormalizeEmail(email);

        return await context.Controllers
            .AsNoTracking()
            .Where(c => c.Id == deviceId && c.User.Email == email)
            .Select(c => new DeviceDto(c.Id, c.Name))
            .FirstOrDefaultAsync();
    }

    public async Task<DeviceDto> CreateDeviceAsync(string email, string deviceName)
    {
        email = InputGuards.NormalizeEmail(email);
        InputGuards.ValidateNameOrThrow(deviceName, "Device name");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            throw new KeyNotFoundException($"User '{email}' not found. Create via POST /api/users.");

        var device = new ControllerDevice
        {
            Name = deviceName.Trim(),
            UserId = user.Id
        };

        context.Controllers.Add(device);
        await context.SaveChangesAsync();

        logger.LogInformation("Device {DeviceId} created for user {Email}", device.Id, email);
        return new DeviceDto(device.Id, device.Name);
    }

    public async Task<bool> UpdateDeviceAsync(string email, int deviceId, string newName)
    {
        email = InputGuards.NormalizeEmail(email);
        InputGuards.ValidateNameOrThrow(newName, "Device name");

        var device = await context.Controllers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == deviceId && c.User.Email == email);

        if (device == null) return false;

        device.Name = newName.Trim();
        await context.SaveChangesAsync();

        logger.LogInformation("Device {DeviceId} updated for user {Email}", deviceId, email);
        return true;
    }

    public async Task<bool> DeleteDeviceAsync(string email, int deviceId)
    {
        email = InputGuards.NormalizeEmail(email);

        var device = await context.Controllers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == deviceId && c.User.Email == email);

        if (device == null) return false;

        context.Controllers.Remove(device);
        await context.SaveChangesAsync();

        logger.LogInformation("Device {DeviceId} deleted for user {Email}", deviceId, email);
        return true;
    }
}
