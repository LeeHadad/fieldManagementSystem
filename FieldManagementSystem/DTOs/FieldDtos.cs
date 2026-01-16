namespace FieldManagementSystem.DTOs;

public record FieldDto(int Id, string Name);
public record CreateFieldRequest(string Name);
public record UpdateFieldRequest(string Name);
