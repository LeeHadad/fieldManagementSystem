using FieldManagementSystem.DTOs;

namespace FieldManagementSystem.Interfaces;

public interface IFieldService
{
    Task<List<FieldDto>> GetFieldsByEmailAsync(string email);
    Task<FieldDto?> GetFieldByIdAsync(string email, int fieldId);
    Task<FieldDto> CreateFieldAsync(string email, string fieldName);
    Task<bool> UpdateFieldAsync(string email, int fieldId, string newName);
    Task<bool> DeleteFieldAsync(string email, int fieldId);
}