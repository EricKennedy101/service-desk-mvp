using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ServiceDeskBackend.Tests.Support;
using Xunit;

namespace ServiceDeskBackend.Tests.Tickets;

public class TicketsApiContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TicketsApiContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_ticket_requires_api_key()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/tickets", BuildTicketPayload());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ticket_rejects_wrong_api_key()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "wrong-key");
        var response = await client.PostAsJsonAsync("/api/tickets", BuildTicketPayload());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ticket_creates_ticket_with_valid_api_key()
    {
        using var client = await CreateAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync("/api/tickets", BuildTicketPayload());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("id", out _));
        Assert.Equal("Open", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Get_ticket_by_id_requires_api_key()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ticket_by_id_returns_ticket()
    {
        using var client = await CreateAuthorizedClientAsync();
        var ticketId = await CreateTicketAsync(client);

        var response = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ticketId.ToString(), json.GetProperty("id").GetString());
        Assert.Equal("Aj123@yahoo.com", json.GetProperty("requesterEmail").GetString());
        Assert.Equal("Open", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Get_mine_requires_api_key()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tickets/mine");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_mine_returns_created_ticket()
    {
        using var client = await CreateAuthorizedClientAsync();
        var ticketId = await CreateTicketAsync(client);

        var response = await client.GetAsync("/api/tickets/mine");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, json.ValueKind);

        var contains = json.EnumerateArray()
            .Any(ticket => ticket.GetProperty("id").GetString() == ticketId.ToString());
        Assert.True(contains);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test-key");
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Guid> CreateTicketAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/tickets", BuildTicketPayload());
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(json.GetProperty("id").GetString() ?? string.Empty);
    }

    private static object BuildTicketPayload()
    {
        return new
        {
            title = "Email access",
            description = "Unable to access email",
            category = "Account",
            priority = "Medium",
            requesterEmail = "test.user@company.com"
        };
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "Aj123@yahoo.com",
            password = "123"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = payload.GetProperty("token").GetString();
        return token ?? string.Empty;
    }
}
