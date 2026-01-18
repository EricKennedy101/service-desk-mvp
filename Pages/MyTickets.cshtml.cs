using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FRAServiceRequestPortal.Pages;

public class MyTicketsModel : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect("/tickets");
    }
}
