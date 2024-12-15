using IssuerDrivingLicense.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IssuerDrivingLicense.Pages.DriverLicenses;

public class CreateModel : PageModel
{
    private readonly DrivingLicenseDbContext _context;

    [FromQuery(Name = "id")]
    public string? Id { get; set; }

    public string? UserName { get; set; }

    [BindProperty]
    public DriverLicense DriverLicense { get; set; } = new DriverLicense();

    public CreateModel(DrivingLicenseDbContext context)
    {
        _context = context;
    }

    public IActionResult OnGet(string id)
    {
        DriverLicense.UserName = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        DriverLicense.Issuedby = HttpContext.User?.Identity?.Name;
        DriverLicense.IssueDate = DateTimeOffset.UtcNow;
        // TODO add logic in UI
        DriverLicense.ExpiryDate = DateTimeOffset.UtcNow.AddYears(10);
        DriverLicense.IssuingCountry = "CH";
        // TODO add logic for county
        DriverLicense.IssuingAuthority = "BE";
        DriverLicense.DocumentNumber = DriverLicenseService.GetRandomString();
        DriverLicense.AdministrativeNumber = DriverLicenseService.GetRandomString();
        DriverLicense.UnDistinguishingSign = "CH";

        // TODO needs to be a json from spec format
        //DrivingPrivileges
        // TODO add other properties as needed

        _context.DriverLicenses.Add(DriverLicense);
        await _context.SaveChangesAsync();

        return RedirectToPage("./User", new { id = DriverLicense.UserName });
    }
}
