using FRAServiceRequestPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FRAServiceRequestPortal.Pages;

public class CasesModel : PageModel
{
    private const string SessionKey = "IsLoggedIn";
    private readonly BackendApiClient _backendApiClient;
    private readonly IConfiguration _configuration;

    public CasesModel(BackendApiClient backendApiClient, IConfiguration configuration)
    {
        _backendApiClient = backendApiClient;
        _configuration = configuration;
    }

    public int? TicketCount { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        if (HttpContext.Session.GetString(SessionKey) != "true")
        {
            return RedirectToPage("/Login");
        }

        if (IsPortalOnly())
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                TicketCount = await _backendApiClient.GetCasesCountAsync(token);
            }
        }

        return Page();
    }

    private bool IsPortalOnly()
    {
        return !string.IsNullOrWhiteSpace(_configuration["BackendApi:BaseUrl"]);
    }
}
