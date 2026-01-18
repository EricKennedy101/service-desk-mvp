using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FRAServiceRequestPortal.Services;

public class BackendApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BackendApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        if (!HasValidBaseAddress(client))
        {
            return null;
        }

        try
        {
            var response = await client.PostAsJsonAsync("/api/Auth/login", new { email, password }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken);
            return payload?.Token;
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool ok, string? error)> GetApiHealthAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        if (!HasValidBaseAddress(client))
        {
            return (false, "Backend API not configured");
        }

        try
        {
            var response = await client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode ? (true, null) : (false, null);
        }
        catch
        {
            return (false, null);
        }
    }

    public async Task<(bool ok, string? error)> GetDbHealthAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        if (!HasValidBaseAddress(client))
        {
            return (false, "Backend API not configured");
        }

        try
        {
            var response = await client.GetAsync("/health/db", cancellationToken);
            return response.IsSuccessStatusCode ? (true, null) : (false, null);
        }
        catch
        {
            return (false, null);
        }
    }

    public async Task<int?> GetCasesCountAsync(string token, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        if (!HasValidBaseAddress(client))
        {
            return null;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            var response = await client.GetAsync("/api/Cases", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
            if (json.ValueKind == JsonValueKind.Array)
            {
                return json.GetArrayLength();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool ok, Guid? id)> CreateTicketAsync(
        string title,
        string description,
        string category,
        string priority,
        string? requesterEmail,
        string? transcriptJson = null,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        if (!HasValidBaseAddress(client))
        {
            return (false, null);
        }

        try
        {
            var payload = new
            {
                title,
                description,
                category,
                priority,
                transcriptJson,
                requesterEmail
            };

            var response = await client.PostAsJsonAsync("/api/tickets", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
            if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("id", out var idValue))
            {
                if (Guid.TryParse(idValue.GetString(), out var id))
                {
                    return (true, id);
                }
            }

            return (true, null);
        }
        catch
        {
            return (false, null);
        }
    }

    private sealed class LoginResponse
    {
        public string? Token { get; set; }
    }

    private static bool HasValidBaseAddress(HttpClient client)
    {
        return client.BaseAddress is { IsAbsoluteUri: true };
    }
}
