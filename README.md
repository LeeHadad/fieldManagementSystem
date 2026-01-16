# Field Management System

RESTful API for managing agricultural fields and devices, built with ASP.NET Core 9.0.

## Features

- User, Field, and Device management (CRUD operations)
- Row-level security (users can only access their own resources)
- Email validation and normalization
- Global exception handling
- Swagger documentation (Development mode)

## Prerequisites

- .NET 9.0 SDK or later

## Quick Start

```bash
# Clone and navigate
git clone <repository-url>
cd FieldManagementSystem

# Restore and build
dotnet restore
dotnet build

# Run
cd FieldManagementSystem
dotnet run
```

API available at `https://localhost:5001`  
Swagger UI: `https://localhost:5001/swagger` (Development mode)

## Configuration

Database connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=FieldManagement.db"
  }
}
```

## API Usage

### Authentication

All endpoints (except `POST /api/users`) require the `X-User-Email` header:
```
X-User-Email: user@example.com
```

### Endpoints

#### Users
- `POST /api/users` - Create user
- `GET /api/users/me` - Get current user

#### Fields
- `GET /api/fields` - List fields
- `GET /api/fields/{id}` - Get field by ID
- `POST /api/fields` - Create field
- `PUT /api/fields/{id}` - Update field
- `DELETE /api/fields/{id}` - Delete field

#### Devices
- `GET /api/devices` - List devices
- `GET /api/devices/{id}` - Get device by ID
- `POST /api/devices` - Create device
- `PUT /api/devices/{id}` - Update device
- `DELETE /api/devices/{id}` - Delete device

### Example Request

```http
POST /api/fields
Content-Type: application/json
X-User-Email: user@example.com

{
  "name": "Tomatoes Field"
}
```

### Error Responses

All errors return:
```json
{
  "error": "Error message"
}
```

Status codes: `400` (Bad Request), `401` (Unauthorized), `404` (Not Found), `409` (Conflict), `500` (Internal Server Error)

## Validation Rules

- **Email**: Valid email format (automatically normalized to lowercase)
- **Names**: Required, max 100 characters

## Testing

```bash
dotnet test
```

40+ integration tests covering all endpoints, validation, and security.

## Project Structure

```
FieldManagementSystem/
├── Controllers/     # API endpoints
├── Services/        # Business logic
├── Interfaces/      # Service contracts
├── Data/           # Database context
├── Models/         # Domain models
├── DTOs/           # Data transfer objects
├── Middlewares/    # Auth & exception handling
└── Utilities/      # Validation helpers
```

## Technologies

- .NET 9.0
- ASP.NET Core
- Entity Framework Core
- SQLite
- Swagger/OpenAPI
- xUnit (testing)
