using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskAgent.DataAccess.Sql;

namespace TaskAgent.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Uses in-memory database instead of real SQL Server.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Set configuration via UseSetting - this is read by builder.Configuration
        builder.UseSetting("DataStore:Type", "InMemory");
        builder.UseSetting("DataStore:DatabaseName", _databaseName);

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext options builder (if SQL was registered before our config took effect)
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            // Also remove any other DbContextOptions
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();

            // Remove and re-add DbContext with in-memory database
            services.RemoveAll<AppDbContext>();
            
            // Add fresh in-memory database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}
