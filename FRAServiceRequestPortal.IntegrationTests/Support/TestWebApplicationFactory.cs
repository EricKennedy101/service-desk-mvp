using System.Net.Http.Headers;
using FRAServiceRequestPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FRAServiceRequestPortal.IntegrationTests.Support;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tickets:RequireAuth"] = "true",
                ["BackendApi:BaseUrl"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

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

    public HttpClient CreateClientWithUser(string email, string roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", email);
        client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        return client;
    }
}
