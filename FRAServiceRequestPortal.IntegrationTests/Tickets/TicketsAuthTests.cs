using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FRAServiceRequestPortal.IntegrationTests.Support;
using Xunit;

namespace FRAServiceRequestPortal.IntegrationTests.Tickets;

public class TicketsAuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TicketsAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_tickets_requires_authentication()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Email access",
            description = "Unable to access email",
            category = "Account",
            priority = "Medium"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_tickets_forbidden_for_wrong_role()
    {
        using var client = _factory.CreateClientWithUser("user@company.com", "Employee");
        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Email access",
            description = "Unable to access email",
            category = "Account",
            priority = "Medium"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_tickets_allows_authorized_user()
    {
        using var client = _factory.CreateClientWithUser("agent@company.com", "SOCAnalyst");
        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Email access",
            description = "Unable to access email",
            category = "Account",
            priority = "Medium"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("id", out _));
        Assert.Equal("Open", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Get_mine_requires_authentication()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tickets/mine");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_mine_forbidden_for_wrong_role()
    {
        using var client = _factory.CreateClientWithUser("user@company.com", "Employee");
        var response = await client.GetAsync("/api/tickets/mine");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_mine_returns_tickets_for_authorized_user()
    {
        using var client = _factory.CreateClientWithUser("agent@company.com", "SOCAnalyst");
        var createResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Laptop issue",
            description = "Laptop is overheating",
            category = "Hardware",
            priority = "High"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var response = await client.GetAsync("/api/tickets/mine");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_requires_authentication()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_forbidden_for_wrong_role()
    {
        using var client = _factory.CreateClientWithUser("user@company.com", "Employee");
        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_returns_ticket_for_authorized_user()
    {
        using var client = _factory.CreateClientWithUser("agent@company.com", "SOCAnalyst");
        var createResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = "VPN access",
            description = "VPN not connecting",
            category = "Network",
            priority = "Medium"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var json = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/tickets/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
