namespace FieldManagementSystem.DTOs;


public record DeviceDto(int Id, string Name);
public record CreateDeviceRequest(string Name);
public record UpdateDeviceRequest(string Name);
