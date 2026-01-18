using FRAServiceRequestPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ServiceDeskBackend.Tests.Support;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Staging");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceDesk:ApiKey"] = "test-key",
                ["BackendApi:BaseUrl"] = "",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Key"] = "test-key-1234567890-test-key-1234567890",
                ["Jwt:ExpiresMinutes"] = "60",
                ["AuthUsers:0:Email"] = "Aj123@yahoo.com",
                ["AuthUsers:0:Password"] = "123",
                ["AuthUsers:0:Roles:0"] = "SOCAnalyst"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions<SqlServerAppDbContext>>();
            services.RemoveAll<SqlServerAppDbContext>();
            services.RemoveAll<AppDbContext>();

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            services.AddSingleton(connection);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            services.AddDbContext<SqlServerAppDbContext>(options => options.UseSqlite(connection));

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            appDb.Database.EnsureCreated();
        });
    }
}
