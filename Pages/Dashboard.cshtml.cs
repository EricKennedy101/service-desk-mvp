using FRAServiceRequestPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FRAServiceRequestPortal.Pages;

public class DashboardModel : PageModel
{
    private const string SessionKey = "IsLoggedIn";
    private readonly BackendApiClient _backendApiClient;
    private readonly IConfiguration _configuration;

    public DashboardModel(BackendApiClient backendApiClient, IConfiguration configuration)
    {
        _backendApiClient = backendApiClient;
        _configuration = configuration;
    }

    public string ApiStatus { get; private set; } = "fail";
    public string DbStatus { get; private set; } = "fail";
    public string? ApiError { get; private set; }
    public string? DbError { get; private set; }
    public int? TicketCount { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        if (!IsLoggedIn())
        {
            return RedirectToPage("/Login");
        }

        if (!IsPortalOnly())
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            ApiStatus = await IsOkAsync(client, $"{baseUrl}/health") ? "ok" : "fail";
            DbStatus = await IsOkAsync(client, $"{baseUrl}/health/db") ? "ok" : "fail";
            ApiError = null;
            DbError = null;
            return Page();
        }

        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Login");
        }

        var api = await _backendApiClient.GetApiHealthAsync();
        ApiStatus = api.ok ? "ok" : "fail";
        ApiError = api.error;

        var db = await _backendApiClient.GetDbHealthAsync();
        DbStatus = db.ok ? "ok" : "fail";
        DbError = db.error;

        TicketCount = await _backendApiClient.GetCasesCountAsync(token);

        return Page();
    }

    private bool IsLoggedIn()
    {
        return HttpContext.Session.GetString(SessionKey) == "true";
    }

    private bool IsPortalOnly()
    {
        return !string.IsNullOrWhiteSpace(_configuration["BackendApi:BaseUrl"]);
    }

    private static async Task<bool> IsOkAsync(HttpClient client, string url)
    {
        try
        {
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
