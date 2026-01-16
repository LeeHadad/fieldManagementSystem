namespace FieldManagementSystem.DTOs;

public record CreateUserRequest(string Email);
public record UserDto(int Id, string Email);