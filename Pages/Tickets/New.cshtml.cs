using FRAServiceRequestPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FRAServiceRequestPortal.Pages.Tickets;

public class NewModel : PageModel
{
    private readonly BackendApiClient _backendApiClient;
    private readonly IConfiguration _configuration;

    public NewModel(BackendApiClient backendApiClient, IConfiguration configuration)
    {
        _backendApiClient = backendApiClient;
        _configuration = configuration;
    }

    [BindProperty]
    public string RequesterEmail { get; set; } = string.Empty;

    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string? TranscriptText { get; set; }

    [BindProperty]
    public string Category { get; set; } = "General";

    [BindProperty]
    public string Priority { get; set; } = "Medium";

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (!IsPortalOnly())
        {
            return RedirectToPage("/Login");
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!IsPortalOnly())
        {
            return RedirectToPage("/Login");
        }

        if (string.IsNullOrWhiteSpace(Title) ||
            string.IsNullOrWhiteSpace(Description) ||
            string.IsNullOrWhiteSpace(Category) ||
            string.IsNullOrWhiteSpace(Priority) ||
            string.IsNullOrWhiteSpace(RequesterEmail))
        {
            ErrorMessage = "All fields are required.";
            return Page();
        }

        var result = await _backendApiClient.CreateTicketAsync(
            Title.Trim(),
            Description.Trim(),
            Category.Trim(),
            Priority.Trim(),
            RequesterEmail.Trim(),
            string.IsNullOrWhiteSpace(TranscriptText) ? null : TranscriptText.Trim());

        if (!result.ok)
        {
            ErrorMessage = "Unable to submit ticket right now.";
            return Page();
        }

        SuccessMessage = "Ticket submitted successfully.";
        ModelState.Clear();
        RequesterEmail = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
        TranscriptText = string.Empty;
        Category = "General";
        Priority = "Medium";
        return Page();
    }

    private bool IsPortalOnly()
    {
        return !string.IsNullOrWhiteSpace(_configuration["BackendApi:BaseUrl"]);
    }
}
