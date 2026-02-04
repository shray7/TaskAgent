using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.DataAccess.Extensions;

/// <summary>
/// Extension methods for registering data access services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Entity Framework Core with InMemory or SQL Server database.
    /// Uses SQL Server when DataStore:Type=Sql and server/database are configured.
    /// Supports two modes:
    /// 1. RBAC/Managed Identity: DataStore:SqlServer + DataStore:SqlDatabase (no password - uses DefaultAzureCredential)
    /// 2. Connection string: ConnectionStrings:SqlDb (legacy, for local dev with SQL auth if needed)
    /// Otherwise uses InMemory (development).
    /// </summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var useSql = configuration["DataStore:Type"]?.Equals("Sql", StringComparison.OrdinalIgnoreCase) == true;
        var sqlConnection = configuration.GetConnectionString("SqlDb");
        var sqlServer = configuration["DataStore:SqlServer"];
        var sqlDatabase = configuration["DataStore:SqlDatabase"];

        if (useSql)
        {
            string? connectionString = null;
            if (!string.IsNullOrWhiteSpace(sqlServer) && !string.IsNullOrWhiteSpace(sqlDatabase))
            {
                // RBAC: passwordless connection using managed identity or DefaultAzureCredential
                var host = sqlServer!.Trim().Replace(".database.windows.net", "");
                connectionString =
                    $"Server=tcp:{host}.database.windows.net,1433;" +
                    $"Database={sqlDatabase};" +
                    "Authentication=Active Directory Default;" +
                    "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            }
            else if (!string.IsNullOrWhiteSpace(sqlConnection))
            {
                connectionString = sqlConnection;
            }

            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(3);
                        sqlOptions.CommandTimeout(30);
                    }));
            }
            else
            {
                throw new InvalidOperationException(
                    "DataStore:Type is set to 'Sql', but no SQL connection information was provided. " +
                    "Configure either DataStore:SqlServer and DataStore:SqlDatabase for managed identity/RBAC, " +
                    "or ConnectionStrings:SqlDb for a full connection string.");
            }
        }

        if (!useSql)
        {
            var databaseName = configuration["DataStore:DatabaseName"] ?? $"TaskAgentDb_{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        }

        return services;
    }
}
