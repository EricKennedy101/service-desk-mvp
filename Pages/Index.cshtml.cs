using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FRAServiceRequestPortal.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ApiStatus { get; private set; } = "fail";
    public string DbStatus { get; private set; } = "fail";
    public bool BackendConfigured { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        var isPortalOnly = !string.IsNullOrWhiteSpace(_configuration["BackendApi:BaseUrl"]);
        if (isPortalOnly)
        {
            return RedirectToPage("/Support/Index");
        }

        BackendConfigured = true;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        ApiStatus = await IsOkAsync(client, $"{baseUrl}/health") ? "ok" : "fail";
        DbStatus = await IsOkAsync(client, $"{baseUrl}/health/db") ? "ok" : "fail";
        return Page();
    }

    private bool IsBackendConfigured()
    {
        var baseUrl = _configuration["BackendApi:BaseUrl"];
        return !string.IsNullOrWhiteSpace(baseUrl)
               && Uri.TryCreate(baseUrl, UriKind.Absolute, out _);
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
