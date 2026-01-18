using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ServiceDeskBackend.Tests.Support;
using Xunit;

namespace ServiceDeskBackend.Tests.Auth;

public class AuthApiContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthApiContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_without_api_key_is_not_blocked_by_api_key_middleware()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "Aj123@yahoo.com",
            password = "123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_then_me_returns_profile()
    {
        using var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "Aj123@yahoo.com",
            password = "123"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var payload = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = payload.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meResponse = await client.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }
}
