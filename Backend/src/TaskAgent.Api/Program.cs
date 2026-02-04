using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskAgent.Api.Filters;
using TaskAgent.Api.Middleware;
using TaskAgent.Api.Options;
using TaskAgent.Api.Services;
using TaskAgent.Api.Validators;
using TaskAgent.Contracts.Dtos;
using TaskAgent.DataAccess.Extensions;
using TaskAgent.DataAccess.Sql;

var builder = WebApplication.CreateBuilder(args);

// Logging: Local (Development) logs to console via default host logger (see appsettings.Development).
// For staging/production: add Serilog/NLog/ApplicationInsights in Program.cs and configure sinks when ready.

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskAgent API",
        Version = "v1",
        Description = "Backend API for TaskAgent - task management (projects, sprints, tasks, comments)"
    });
});

// FluentValidation: register all validators from this assembly and global action filter
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationActionFilter>();
});

// Add password hasher and JWT token service for authentication
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key ?? ""))
        };
    });

// Add data access layer (EF Core InMemory)
builder.Services.AddDataAccess(builder.Configuration);

// Task management services
builder.Services.AddScoped<ICommentsService, CommentsService>();
builder.Services.AddScoped<IProjectsService, ProjectsService>();
builder.Services.AddScoped<ISprintsService, SprintsService>();
builder.Services.AddScoped<ITasksService, TasksService>();
builder.Services.AddScoped<IUsersService, UsersService>();

// Realtime (Socket.IO) notifier - optional; set Realtime:ServerUrl to enable
builder.Services.Configure<RealtimeOptions>(builder.Configuration.GetSection(RealtimeOptions.SectionName));
builder.Services.AddHttpClient<IBoardRealtimeNotifier, BoardRealtimeNotifier>();

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

// Rate limiting: fixed window per IP (100 requests per minute); 429 with ApiErrorDto when rejected
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ApiErrorDto("Too many requests."), ct);
    };
});

var app = builder.Build();

// Correlation ID first so it is available in exception handler and request logging
app.UseMiddleware<CorrelationIdMiddleware>();

// Global exception handling: log and return consistent JSON error
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;
        var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] as string ?? "-";
        if (ex != null)
            app.Logger.LogError(ex, "Unhandled exception (CorrelationId: {CorrelationId})", correlationId);
        var message = app.Environment.IsDevelopment() && ex != null ? ex.Message : "An unexpected error occurred.";
        var body = new ApiErrorDto(message);
        await context.Response.WriteAsJsonAsync(body);
    });
});

// Request logging (after exception handler; correlation ID already set)
app.UseMiddleware<RequestLoggingMiddleware>();

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for easier API exploration
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskAgent API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseRateLimiter();
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

// Health check endpoints for Azure Container Apps (excluded from rate limiting)
// Liveness probe - confirms the app process is running (no dependency checks)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
}).DisableRateLimiting();

// Readiness probe - verifies all dependencies are healthy
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
}).DisableRateLimiting();

// Default /health endpoint for backward compatibility (same as ready)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponse
}).DisableRateLimiting();

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
