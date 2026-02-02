using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskAgent.Api.Services;
using TaskAgent.DataAccess.Extensions;
using TaskAgent.DataAccess.Sql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskAgent API",
        Version = "v1",
        Description = "Backend API for TaskAgent - manages jobs and companies for subcontractor scheduling"
    });
});
builder.Services.AddControllers();

// Add password hasher for secure authentication
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// Add data access layer (EF Core InMemory)
builder.Services.AddDataAccess(builder.Configuration);

// CORS: localhost (dev) + Cors:AllowedOrigins (e.g. GitHub Pages)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var originsConfig = builder.Configuration["Cors:AllowedOrigins"] ?? "";
        var allowedOrigins = originsConfig
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var allowedSet = new HashSet<string>(allowedOrigins, StringComparer.OrdinalIgnoreCase);

        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                try
                {
                    var uri = new Uri(origin);
                    if (uri.Host is "localhost" or "127.0.0.1") return true;
                    return allowedSet.Contains(origin.TrimEnd('/')) || allowedSet.Contains(origin);
                }
                catch { return false; }
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configure health checks with EF Core DbContext check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "db"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for easier API exploration
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskAgent API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();

// Seed task management data (Vue app compatibility) - non-blocking so app starts even if seed fails
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TaskManagementSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Task management seed failed - app will start without demo data");
    }
}

// Health check endpoints for Azure Container Apps
// Liveness probe - confirms the app process is running (no dependency checks)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

// Readiness probe - verifies all dependencies are healthy
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

// Default /health endpoint for backward compatibility (same as ready)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponse
});

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    
    var response = new
    {
        status = report.Status.ToString(),
        timestamp = DateTime.UtcNow,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.TotalMilliseconds
        })
    };
    
    return context.Response.WriteAsJsonAsync(response);
}

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
