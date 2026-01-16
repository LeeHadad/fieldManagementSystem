using FieldManagementSystem.DTOs;

namespace FieldManagementSystem.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(string email);
    Task<UserDto?> GetUserByEmailAsync(string email);
}