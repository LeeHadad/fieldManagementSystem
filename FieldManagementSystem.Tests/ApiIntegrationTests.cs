using System.Net;
using System.Net.Http.Json;
using System.Threading;
using FluentAssertions;
using FieldManagementSystem.DTOs;
using Xunit;

namespace FieldManagementSystem.Tests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static int _testCounter = 0;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private string GetUniqueEmail(string prefix = "test")
    {
        return $"{prefix}_{Interlocked.Increment(ref _testCounter)}@example.com";
    }

    [Fact]
    public async Task AnyApiCall_WithoutUserEmailHeader_ShouldReturn401()
    {
        // Act (no header)
        var res = await _client.GetAsync("/api/fields");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("Missing X-User-Email");
    }

    [Fact]
    public async Task ApiCall_WithInvalidEmailFormat_ShouldReturn400()
    {
        // Arrange
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/fields");
        req.Headers.Add("X-User-Email", "invalid-email-format");

        // Act
        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid email format");
    }
    

    [Fact]
    public async Task EmailNormalization_CaseInsensitive_ShouldWork()
    {
        // Arrange - Create user with mixed case email
        var emailLower = GetUniqueEmail("test");
        var emailUpper = emailLower.ToUpperInvariant();

        // Create user with lowercase email (normalized)
        await CreateUser(emailLower);

        // Act - Try to access with uppercase email (should be normalized and match)
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/fields");
        req.Headers.Add("X-User-Email", emailUpper);

        var res = await _client.SendAsync(req);

        // Assert - Should work because email is normalized
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_WithMixedCaseEmail_ShouldNormalizeAndStore()
    {
        // Arrange
        var mixedCaseEmail = $"Test_{Interlocked.Increment(ref _testCounter)}@Example.COM";

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(mixedCaseEmail))
        };
        req.Headers.Add("X-User-Email", mixedCaseEmail);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await res.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(mixedCaseEmail.ToLowerInvariant().Trim());
    }

    [Fact]
    public async Task CreateUser_ThenCreateField_ShouldReturn201_AndFieldReturnedInList()
    {
        // Arrange
        var email = GetUniqueEmail("farmer");

        // Create user (middleware is excluded for /api/users via UseWhen, but we pass header anyway)
        var userReq = new CreateUserRequest(email);
        using var createUser = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(userReq)
        };
        createUser.Headers.Add("X-User-Email", email);

        // Act 1: create user
        var userRes = await _client.SendAsync(createUser);
        userRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 2: create field
        using var createField = new HttpRequestMessage(HttpMethod.Post, "/api/fields")
        {
            Content = JsonContent.Create(new CreateFieldRequest("Tomatoes Field"))
        };
        createField.Headers.Add("X-User-Email", email);

        var fieldRes = await _client.SendAsync(createField);
        fieldRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 3: list fields
        using var getFields = new HttpRequestMessage(HttpMethod.Get, "/api/fields");
        getFields.Headers.Add("X-User-Email", email);

        var listRes = await _client.SendAsync(getFields);
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var fields = await listRes.Content.ReadFromJsonAsync<List<FieldDto>>();
        fields.Should().NotBeNull();
        fields!.Should().ContainSingle(f => f.Name == "Tomatoes Field");
    }

    [Fact]
    public async Task RowLevelAccess_UserB_CannotSeeUserA_Field()
    {
        // Arrange
        var userA = GetUniqueEmail("userA");
        var userB = GetUniqueEmail("userB");

        // Create both users
        await CreateUser(userA);
        await CreateUser(userB);

        // Create field for userA
        using var createFieldA = new HttpRequestMessage(HttpMethod.Post, "/api/fields")
        {
            Content = JsonContent.Create(new CreateFieldRequest("A-Field"))
        };
        createFieldA.Headers.Add("X-User-Email", userA);

        var createARes = await _client.SendAsync(createFieldA);
        createARes.StatusCode.Should().Be(HttpStatusCode.Created);

        // UserB lists fields
        using var getFieldsB = new HttpRequestMessage(HttpMethod.Get, "/api/fields");
        getFieldsB.Headers.Add("X-User-Email", userB);

        var listBRes = await _client.SendAsync(getFieldsB);
        listBRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var fieldsB = await listBRes.Content.ReadFromJsonAsync<List<FieldDto>>();
        fieldsB.Should().NotBeNull();
        fieldsB!.Should().NotContain(f => f.Name == "A-Field");
    }

    [Fact]
    public async Task CreateDevice_WhenUserMissing_ShouldReturn404()
    {
        // Arrange
        var email = GetUniqueEmail("missing");

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/devices")
        {
            Content = JsonContent.Create(new CreateDeviceRequest("Irrigation-1"))
        };
        req.Headers.Add("X-User-Email", email);

        // Act
        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Device Tests
    [Fact]
    public async Task CreateDevice_WhenUserExists_ShouldReturn201()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/devices")
        {
            Content = JsonContent.Create(new CreateDeviceRequest("Irrigation-1"))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var device = await res.Content.ReadFromJsonAsync<DeviceDto>();
        device.Should().NotBeNull();
        device!.Name.Should().Be("Irrigation-1");
    }

    [Fact]
    public async Task GetDeviceById_WhenExists_ShouldReturn200()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);

        // Create device
        var device = await CreateDevice(email, "Test Device");

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/devices/{device.Id}");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrieved = await res.Content.ReadFromJsonAsync<DeviceDto>();
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(device.Id);
        retrieved.Name.Should().Be("Test Device");
    }

    [Fact]
    public async Task GetDeviceById_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/devices/99999");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateDevice_WhenExists_ShouldReturn204()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);
        var device = await CreateDevice(email, "Old Name");

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/devices/{device.Id}")
        {
            Content = JsonContent.Create(new UpdateDeviceRequest("New Name"))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var updated = await GetDeviceById(email, device.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateDevice_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Put, "/api/devices/99999")
        {
            Content = JsonContent.Create(new UpdateDeviceRequest("New Name"))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDevice_WhenExists_ShouldReturn204()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);
        var device = await CreateDevice(email, "To Delete");

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/devices/{device.Id}");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var deleted = await GetDeviceById(email, device.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDevice_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Delete, "/api/devices/99999");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RowLevelAccess_UserB_CannotSeeUserA_Device()
    {
        // Arrange
        var userA = GetUniqueEmail("userA");
        var userB = GetUniqueEmail("userB");
        await CreateUser(userA);
        await CreateUser(userB);

        var deviceA = await CreateDevice(userA, "A-Device");

        // Act - UserB tries to get UserA's device
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/devices/{deviceA.Id}");
        req.Headers.Add("X-User-Email", userB);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RowLevelAccess_UserB_CannotUpdateUserA_Device()
    {
        // Arrange
        var userA = GetUniqueEmail("userA");
        var userB = GetUniqueEmail("userB");
        await CreateUser(userA);
        await CreateUser(userB);

        var deviceA = await CreateDevice(userA, "A-Device");

        // Act - UserB tries to update UserA's device
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/devices/{deviceA.Id}")
        {
            Content = JsonContent.Create(new UpdateDeviceRequest("Hacked"))
        };
        req.Headers.Add("X-User-Email", userB);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RowLevelAccess_UserB_CannotDeleteUserA_Device()
    {
        // Arrange
        var userA = GetUniqueEmail("userA");
        var userB = GetUniqueEmail("userB");
        await CreateUser(userA);
        await CreateUser(userB);

        var deviceA = await CreateDevice(userA, "A-Device");

        // Act - UserB tries to delete UserA's device
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/devices/{deviceA.Id}");
        req.Headers.Add("X-User-Email", userB);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Field Tests
    [Fact]
    public async Task GetFieldById_WhenExists_ShouldReturn200()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);
        var field = await CreateField(email, "Test Field");

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/fields/{field.Id}");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrieved = await res.Content.ReadFromJsonAsync<FieldDto>();
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(field.Id);
        retrieved.Name.Should().Be("Test Field");
    }

    [Fact]
    public async Task GetFieldById_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/fields/99999");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateField_WhenExists_ShouldReturn204()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);
        var field = await CreateField(email, "Old Name");

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/fields/{field.Id}")
        {
            Content = JsonContent.Create(new UpdateFieldRequest("New Name"))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var updated = await GetFieldById(email, field.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateField_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Put, "/api/fields/99999")
        {
            Content = JsonContent.Create(new UpdateFieldRequest("New Name"))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteField_WhenExists_ShouldReturn204()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);
        var field = await CreateField(email, "To Delete");

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/fields/{field.Id}");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var deleted = await GetFieldById(email, field.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteField_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Delete, "/api/fields/99999");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RowLevelAccess_UserB_CannotUpdateUserA_Field()
    {
        // Arrange
        var userA = GetUniqueEmail("userA");
        var userB = GetUniqueEmail("userB");
        await CreateUser(userA);
        await CreateUser(userB);

        var fieldA = await CreateField(userA, "A-Field");

        // Act - UserB tries to update UserA's field
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/fields/{fieldA.Id}")
        {
            Content = JsonContent.Create(new UpdateFieldRequest("Hacked"))
        };
        req.Headers.Add("X-User-Email", userB);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RowLevelAccess_UserB_CannotDeleteUserA_Field()
    {
        // Arrange
        var userA = GetUniqueEmail("userA");
        var userB = GetUniqueEmail("userB");
        await CreateUser(userA);
        await CreateUser(userB);

        var fieldA = await CreateField(userA, "A-Field");

        // Act - UserB tries to delete UserA's field
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/fields/{fieldA.Id}");
        req.Headers.Add("X-User-Email", userB);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Validation Tests
    [Fact]
    public async Task CreateField_EmptyName_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/fields")
        {
            Content = JsonContent.Create(new CreateFieldRequest(""))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateField_WhitespaceName_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/fields")
        {
            Content = JsonContent.Create(new CreateFieldRequest("   "))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateField_NameTooLong_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);
        var longName = new string('A', 101); // 101 characters

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/fields")
        {
            Content = JsonContent.Create(new CreateFieldRequest(longName))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDevice_EmptyName_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/devices")
        {
            Content = JsonContent.Create(new CreateDeviceRequest(""))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDevice_WhitespaceName_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/devices")
        {
            Content = JsonContent.Create(new CreateDeviceRequest("   "))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDevice_NameTooLong_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);
        var longName = new string('A', 101); // 101 characters

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/devices")
        {
            Content = JsonContent.Create(new CreateDeviceRequest(longName))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateField_EmptyName_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);
        var field = await CreateField(email, "Test Field");

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/fields/{field.Id}")
        {
            Content = JsonContent.Create(new UpdateFieldRequest(""))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateDevice_EmptyName_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);
        var device = await CreateDevice(email, "Test Device");

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/devices/{device.Id}")
        {
            Content = JsonContent.Create(new UpdateDeviceRequest(""))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("Device name is required");
    }

    [Fact]
    public async Task UpdateDevice_NameTooLong_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("device");
        await CreateUser(email);
        var device = await CreateDevice(email, "Test Device");
        var longName = new string('A', 101); // 101 characters

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/devices/{device.Id}")
        {
            Content = JsonContent.Create(new UpdateDeviceRequest(longName))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("cannot exceed 100 characters");
    }

    [Fact]
    public async Task UpdateField_NameTooLong_ShouldReturn400()
    {
        // Arrange
        var email = GetUniqueEmail("field");
        await CreateUser(email);
        var field = await CreateField(email, "Test Field");
        var longName = new string('A', 101); // 101 characters

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/fields/{field.Id}")
        {
            Content = JsonContent.Create(new UpdateFieldRequest(longName))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("cannot exceed 100 characters");
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ShouldReturn400()
    {
        // Arrange
        var invalidEmail = "not-an-email";

        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(invalidEmail))
        };
        req.Headers.Add("X-User-Email", invalidEmail);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid email format");
    }

    [Fact]
    public async Task CreateUser_WithEmptyEmail_ShouldReturn400()
    {
        // Arrange
        // Act
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(""))
        };
        req.Headers.Add("X-User-Email", "");

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ShouldReturn409()
    {
        // Arrange
        var email = GetUniqueEmail("duplicate");
        await CreateUser(email);

        // Act - Try to create same user again
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(email))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("User already exists");
    }

    // Helper Methods
    private async Task CreateUser(string email)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(email))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);

        // If user already exists in a test run, it could be Conflict; accept both.
        (res.StatusCode == HttpStatusCode.Created || res.StatusCode == HttpStatusCode.Conflict)
            .Should().BeTrue($"Expected 201 or 409, got {(int)res.StatusCode}");
    }

    private async Task<DeviceDto> CreateDevice(string email, string deviceName)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/devices")
        {
            Content = JsonContent.Create(new CreateDeviceRequest(deviceName))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var device = await res.Content.ReadFromJsonAsync<DeviceDto>();
        device.Should().NotBeNull();
        return device!;
    }

    private async Task<DeviceDto?> GetDeviceById(string email, int deviceId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/devices/{deviceId}");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);
        
        if (res.StatusCode == HttpStatusCode.NotFound)
            return null;

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        return await res.Content.ReadFromJsonAsync<DeviceDto>();
    }

    private async Task<FieldDto> CreateField(string email, string fieldName)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/fields")
        {
            Content = JsonContent.Create(new CreateFieldRequest(fieldName))
        };
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var field = await res.Content.ReadFromJsonAsync<FieldDto>();
        field.Should().NotBeNull();
        return field!;
    }

    private async Task<FieldDto?> GetFieldById(string email, int fieldId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/fields/{fieldId}");
        req.Headers.Add("X-User-Email", email);

        var res = await _client.SendAsync(req);
        
        if (res.StatusCode == HttpStatusCode.NotFound)
            return null;

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        return await res.Content.ReadFromJsonAsync<FieldDto>();
    }
}
