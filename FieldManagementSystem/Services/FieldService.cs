using FieldManagementSystem.Data;
using FieldManagementSystem.DTOs;
using FieldManagementSystem.Interfaces;
using FieldManagementSystem.Models;
using FieldManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;

namespace FieldManagementSystem.Services;

public class FieldService(AppDbContext context, ILogger<FieldService> logger) : IFieldService
{
    public async Task<List<FieldDto>> GetFieldsByEmailAsync(string email)
    {
        email = InputGuards.NormalizeEmail(email);

        return await context.Fields
            .AsNoTracking()
            .Where(f => f.User.Email == email)
            .Select(f => new FieldDto(f.Id, f.Name))
            .ToListAsync();
    }

    public async Task<FieldDto?> GetFieldByIdAsync(string email, int fieldId)
    {
        email = InputGuards.NormalizeEmail(email);

        return await context.Fields
            .AsNoTracking()
            .Where(f => f.Id == fieldId && f.User.Email == email)
            .Select(f => new FieldDto(f.Id, f.Name))
            .FirstOrDefaultAsync();
    }

    public async Task<FieldDto> CreateFieldAsync(string email, string fieldName)
    {
        email = InputGuards.NormalizeEmail(email);
        InputGuards.ValidateNameOrThrow(fieldName, "Field name");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            throw new KeyNotFoundException($"User '{email}' not found. Create via POST /api/users.");

        var field = new Field
        {
            Name = fieldName.Trim(),
            UserId = user.Id
        };

        context.Fields.Add(field);
        await context.SaveChangesAsync();

        logger.LogInformation("Field {FieldId} created for user {Email}", field.Id, email);
        return new FieldDto(field.Id, field.Name);
    }

    public async Task<bool> UpdateFieldAsync(string email, int fieldId, string newName)
    {
        email = InputGuards.NormalizeEmail(email);
        InputGuards.ValidateNameOrThrow(newName, "Field name");

        var field = await context.Fields
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.User.Email == email);

        if (field == null) return false;

        field.Name = newName.Trim();
        await context.SaveChangesAsync();

        logger.LogInformation("Field {FieldId} updated for user {Email}", fieldId, email);
        return true;
    }

    public async Task<bool> DeleteFieldAsync(string email, int fieldId)
    {
        email = InputGuards.NormalizeEmail(email);

        var field = await context.Fields
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.User.Email == email);

        if (field == null) return false;

        context.Fields.Remove(field);
        await context.SaveChangesAsync();

        logger.LogInformation("Field {FieldId} deleted for user {Email}", fieldId, email);
        return true;
    }
}
