using FRAServiceRequestPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FRAServiceRequestPortal.Pages;

public class LoginModel : PageModel
{
    private const string SessionKey = "IsLoggedIn";
    private readonly BackendApiClient _backendApiClient;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public LoginModel(BackendApiClient backendApiClient, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _backendApiClient = backendApiClient;
        _configuration = configuration;
        _environment = environment;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }
    public string EnvironmentBadge { get; private set; } = "PRODUCTION";
    public bool BackendConfigured { get; private set; }
    public string ApiStatus { get; private set; } = "fail";
    public string DbStatus { get; private set; } = "fail";
    public string? ApiError { get; private set; }
    public string? DbError { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        EnvironmentBadge = _environment.IsStaging() ? "STAGING" : "PRODUCTION";
        BackendConfigured = IsBackendConfigured();
        if (IsPortalOnly())
        {
            return RedirectToPage("/Support/Index");
        }
        await LoadHealthStatus();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        EnvironmentBadge = _environment.IsStaging() ? "STAGING" : "PRODUCTION";
        BackendConfigured = IsBackendConfigured();
        if (IsPortalOnly())
        {
            return RedirectToPage("/Support/Index");
        }

        if (!IsPortalOnly())
        {
            if (IsDemoLoginValid(Email, Password))
            {
                HttpContext.Session.SetString(SessionKey, "true");
                return RedirectToPage("/Dashboard");
            }
        }
        else
        {
            var token = await _backendApiClient.LoginAsync(Email, Password);
            if (!string.IsNullOrWhiteSpace(token))
            {
                HttpContext.Session.SetString(SessionKey, "true");
                HttpContext.Session.SetString("UserEmail", Email);
                HttpContext.Session.SetString("AccessToken", token);
                return RedirectToPage("/Dashboard");
            }
        }

        ErrorMessage = "Invalid credentials.";
        await LoadHealthStatus();
        return Page();
    }

    private async Task LoadHealthStatus()
    {
        if (!IsPortalOnly())
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            ApiStatus = await IsOkAsync(client, $"{baseUrl}/health") ? "ok" : "fail";
            DbStatus = await IsOkAsync(client, $"{baseUrl}/health/db") ? "ok" : "fail";
            ApiError = null;
            DbError = null;
            return;
        }

        if (!BackendConfigured)
        {
            ApiStatus = "fail";
            DbStatus = "fail";
            ApiError = null;
            DbError = null;
            return;
        }

        var api = await _backendApiClient.GetApiHealthAsync();
        ApiStatus = api.ok ? "ok" : "fail";
        ApiError = api.error;

        var db = await _backendApiClient.GetDbHealthAsync();
        DbStatus = db.ok ? "ok" : "fail";
        DbError = db.error;
    }

    private bool IsPortalOnly()
    {
        return !string.IsNullOrWhiteSpace(_configuration["BackendApi:BaseUrl"]);
    }

    private bool IsBackendConfigured()
    {
        if (!IsPortalOnly())
        {
            return true;
        }

        var baseUrl = _configuration["BackendApi:BaseUrl"];
        return !string.IsNullOrWhiteSpace(baseUrl)
               && Uri.TryCreate(baseUrl, UriKind.Absolute, out _);
    }

    private static bool IsDemoLoginValid(string email, string password)
    {
        return !string.IsNullOrWhiteSpace(email)
               && email.Contains('@', StringComparison.Ordinal)
               && !string.IsNullOrWhiteSpace(password)
               && password.Length >= 6;
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
