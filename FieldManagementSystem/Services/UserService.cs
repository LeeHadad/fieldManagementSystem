using FieldManagementSystem.Data;
using FieldManagementSystem.DTOs;
using FieldManagementSystem.Interfaces;
using FieldManagementSystem.Models;
using FieldManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;

namespace FieldManagementSystem.Services;

public class UserService(AppDbContext context, ILogger<UserService> logger) : IUserService
{
    public async Task<UserDto> CreateUserAsync(string email)
    {
        // Validate (including normalization inside for format check)
        InputGuards.ValidateEmailOrThrow(email);

        // Normalize once for DB usage
        email = InputGuards.NormalizeEmail(email);

        var exists = await context.Users.AnyAsync(u => u.Email == email);
        if (exists)
            throw new InvalidOperationException("User already exists.");

        var user = new User { Email = email };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        logger.LogInformation("User created: {Email}", email);
        return new UserDto(user.Id, user.Email);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        email = InputGuards.NormalizeEmail(email);

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);

        return user == null ? null : new UserDto(user.Id, user.Email);
    }
}