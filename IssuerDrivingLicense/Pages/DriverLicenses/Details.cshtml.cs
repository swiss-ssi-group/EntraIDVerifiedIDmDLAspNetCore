using IssuerDrivingLicense.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IssuerDrivingLicense.Pages.DriverLicenses;

public class DetailsModel : PageModel
{
    private readonly DrivingLicenseDbContext _context;

    public DetailsModel(DrivingLicenseDbContext context)
    {
        _context = context;
    }

    public DriverLicense? DriverLicense { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        DriverLicense = await _context.DriverLicenses.FirstOrDefaultAsync(m => m.Id == id);

        if (DriverLicense == null)
        {
            return NotFound();
        }

        return Page();
    }
}
