## Solution Structure Plan (C#/.NET)

To convert this codebase into a well-structured solution with multiple projects, we should leverage .NET solution (`.sln`) and project (`.csproj`) files. Here's the overall structure and the projects we will introduce:

---

### Target Directory Structure

```
/TaskAgent.sln
/src
  /TaskAgent.Api                 # Web API (existing backend, renamed)
    - Program.cs
    - Controllers/
    - TaskAgent.Api.csproj
  /TaskAgent.Contracts           # DTOs, request/response models, shared interfaces
    - Dtos/
    - Requests/
    - Responses/
    - TaskAgent.Contracts.csproj
  /TaskAgent.DataAccess          # Repositories, DbContext, data layer
    - Abstractions/
    - Sql/
    - NoSql/
    - TaskAgent.DataAccess.csproj
/tests
  /TaskAgent.Api.Tests           # xUnit tests for the API
    - TaskAgent.Api.Tests.csproj
  /TaskAgent.DataAccess.Tests    # xUnit tests for data access
    - TaskAgent.DataAccess.Tests.csproj
```

---

### Project Dependency Graph

```
┌─────────────────────────────────────────────────────────────┐
│                      TaskAgent.Api                         │
│                    (Web API - Entry Point)                  │
└─────────────────────┬───────────────────┬───────────────────┘
                      │                   │
                      ▼                   ▼
        ┌─────────────────────┐   ┌─────────────────────┐
        │ TaskAgent.Contracts│   │ TaskAgent.DataAccess│
        │      (DTOs)         │   │   (Repositories)     │
        └─────────────────────┘   └───────────┬──────────┘
                      ▲                       │
                      │                       │
                      └───────────────────────┘
                      (DataAccess references Contracts)
```

**Dependencies:**
- `TaskAgent.Api` → references `TaskAgent.Contracts` and `TaskAgent.DataAccess`
- `TaskAgent.DataAccess` → references `TaskAgent.Contracts`
- `TaskAgent.Contracts` → no project dependencies (standalone)
- `TaskAgent.Api.Tests` → references `TaskAgent.Api`
- `TaskAgent.DataAccess.Tests` → references `TaskAgent.DataAccess`

---

### Project Details

#### 1. TaskAgent.Contracts (Class Library)

**Purpose:** Shared data transfer objects, request/response models, and interfaces that can be used across projects (and potentially shared with frontend clients).

**Contents:**
- `Dtos/` - Data Transfer Objects (e.g., `UserDto.cs`, `ProductDto.cs`)
- `Requests/` - API request models (e.g., `CreateUserRequest.cs`)
- `Responses/` - API response models (e.g., `PaginatedResponse.cs`)
- `Interfaces/` - Shared interfaces if needed

**NuGet Packages:** None required (pure C# models)

**Example file - `Dtos/UserDto.cs`:**
```csharp
namespace TaskAgent.Contracts.Dtos;

public record UserDto(
    string Id,
    string Email,
    string DisplayName,
    DateTime CreatedAt
);
```

---

#### 2. TaskAgent.DataAccess (Class Library)

**Purpose:** Data access layer containing repository interfaces, EF Core implementations, DbContext, and database entities.

**Contents:**
- `Abstractions/` - Repository interfaces (`IRepository<T>`, `IUserRepository`, etc.)
- `Entities/` - Database entity classes (internal to data layer)
- `Sql/` - EF Core implementations
  - `AppDbContext.cs`
  - `Repositories/SqlUserRepository.cs`
- `Extensions/` - DI registration extensions (`ServiceCollectionExtensions.cs`)

**NuGet Packages:**
- `Microsoft.EntityFrameworkCore.SqlServer` (or `.Sqlite` for dev)
- `Microsoft.EntityFrameworkCore.Design`

**Example file - `Abstractions/IRepository.cs`:**
```csharp
namespace TaskAgent.DataAccess.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
```

**Example file - `Extensions/ServiceCollectionExtensions.cs`:**
```csharp
namespace TaskAgent.DataAccess.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register DbContext, repositories, etc.
        return services;
    }
}
```

---

#### 3. TaskAgent.Api (Web API - Existing Project, Renamed)

**Purpose:** ASP.NET Core Web API, handles HTTP requests, authentication, and orchestrates calls to the data layer.

**Contents:**
- `Program.cs` - Application entry point and DI configuration
- `Controllers/` - API controllers
- `Middleware/` - Custom middleware (error handling, logging)
- `appsettings.json` - Configuration

**NuGet Packages:**
- `Microsoft.AspNetCore.OpenApi` (existing)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (for auth)
- Project references to `TaskAgent.Contracts` and `TaskAgent.DataAccess`

**Updated `Program.cs` example:**
```csharp
using TaskAgent.DataAccess.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDataAccess(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
```

---

#### 4. TaskAgent.Api.Tests (xUnit Test Project)

**Purpose:** Unit and integration tests for the API layer.

**Contents:**
- `Controllers/` - Controller unit tests
- `Integration/` - Integration tests using `WebApplicationFactory`
- `Fixtures/` - Test fixtures and shared setup

**NuGet Packages:**
- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`
- `Moq` or `NSubstitute` (for mocking)
- `FluentAssertions` (optional, for readable assertions)
- `Microsoft.AspNetCore.Mvc.Testing` (for integration tests)

---

#### 5. TaskAgent.DataAccess.Tests (xUnit Test Project)

**Purpose:** Unit tests for repository implementations and data access logic.

**Contents:**
- `Repositories/` - Repository unit tests
- `Fixtures/` - In-memory database fixtures

**NuGet Packages:**
- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`
- `Microsoft.EntityFrameworkCore.InMemory` (for EF Core testing)

---

### Implementation Steps

#### Step 1: Create Solution and Folder Structure
```bash
# Create solution file
dotnet new sln -n TaskAgent

# Create folder structure
mkdir -p src tests
```

#### Step 2: Create Contracts Project
```bash
dotnet new classlib -n TaskAgent.Contracts -o src/TaskAgent.Contracts
dotnet sln add src/TaskAgent.Contracts/TaskAgent.Contracts.csproj
```

#### Step 3: Create DataAccess Project
```bash
dotnet new classlib -n TaskAgent.DataAccess -o src/TaskAgent.DataAccess
dotnet sln add src/TaskAgent.DataAccess/TaskAgent.DataAccess.csproj

# Add reference to Contracts
dotnet add src/TaskAgent.DataAccess reference src/TaskAgent.Contracts
```

#### Step 4: Move and Rename Existing API Project
```bash
# Move existing project to src folder
mv rapidworks-backend src/TaskAgent.Api

# Update project file name and add to solution
mv src/TaskAgent.Api/rapidworks-backend.csproj src/TaskAgent.Api/TaskAgent.Api.csproj
dotnet sln add src/TaskAgent.Api/TaskAgent.Api.csproj

# Add references
dotnet add src/TaskAgent.Api reference src/TaskAgent.Contracts
dotnet add src/TaskAgent.Api reference src/TaskAgent.DataAccess
```

#### Step 5: Create Test Projects
```bash
# API Tests
dotnet new xunit -n TaskAgent.Api.Tests -o tests/TaskAgent.Api.Tests
dotnet sln add tests/TaskAgent.Api.Tests/TaskAgent.Api.Tests.csproj
dotnet add tests/TaskAgent.Api.Tests reference src/TaskAgent.Api

# DataAccess Tests
dotnet new xunit -n TaskAgent.DataAccess.Tests -o tests/TaskAgent.DataAccess.Tests
dotnet sln add tests/TaskAgent.DataAccess.Tests/TaskAgent.DataAccess.Tests.csproj
dotnet add tests/TaskAgent.DataAccess.Tests reference src/TaskAgent.DataAccess
```

#### Step 6: Add NuGet Packages
```bash
# DataAccess packages
dotnet add src/TaskAgent.DataAccess package Microsoft.EntityFrameworkCore.SqlServer

# Test packages
dotnet add tests/TaskAgent.Api.Tests package Moq
dotnet add tests/TaskAgent.Api.Tests package FluentAssertions
dotnet add tests/TaskAgent.Api.Tests package Microsoft.AspNetCore.Mvc.Testing

dotnet add tests/TaskAgent.DataAccess.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/TaskAgent.DataAccess.Tests package FluentAssertions
```

#### Step 7: Update Namespaces and Imports
- Update `RootNamespace` in each `.csproj` file
- Update `using` statements in existing code

#### Step 8: Verify Build
```bash
dotnet build TaskAgent.sln
dotnet test TaskAgent.sln
```

---

### Final Solution Structure (After Implementation)

```
/TaskAgent/
├── TaskAgent.sln
├── src/
│   ├── TaskAgent.Api/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── TaskAgent.Api.csproj
│   ├── TaskAgent.Contracts/
│   │   ├── Dtos/
│   │   ├── Requests/
│   │   ├── Responses/
│   │   └── TaskAgent.Contracts.csproj
│   └── TaskAgent.DataAccess/
│       ├── Abstractions/
│       ├── Entities/
│       ├── Extensions/
│       ├── Sql/
│       ├── NoSql/
│       └── TaskAgent.DataAccess.csproj
└── tests/
    ├── TaskAgent.Api.Tests/
    │   ├── Controllers/
    │   ├── Integration/
    │   └── TaskAgent.Api.Tests.csproj
    └── TaskAgent.DataAccess.Tests/
        ├── Repositories/
        └── TaskAgent.DataAccess.Tests.csproj
```

---

### Notes

- **Contracts as a separate project** allows sharing DTOs with frontend clients (can be published as a NuGet package)
- **DataAccess isolation** keeps database concerns out of the API layer
- **Test projects per source project** enables focused testing and faster test runs
- **Extensions pattern** (`AddDataAccess()`) keeps `Program.cs` clean and testable
