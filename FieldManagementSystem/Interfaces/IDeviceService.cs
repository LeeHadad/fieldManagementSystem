using FieldManagementSystem.DTOs;

namespace FieldManagementSystem.Interfaces;

public interface IDeviceService
{
    Task<List<DeviceDto>> GetDevicesByEmailAsync(string email);
    Task<DeviceDto?> GetDeviceByIdAsync(string email, int deviceId);
    Task<DeviceDto> CreateDeviceAsync(string email, string deviceName);
    Task<bool> UpdateDeviceAsync(string email, int deviceId, string newName);
    Task<bool> DeleteDeviceAsync(string email, int deviceId);
}