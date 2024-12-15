using IssuerDrivingLicense.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IssuerDrivingLicense.Pages.DriverLicenses;

public class EditModel : PageModel
{
    private readonly DrivingLicenseDbContext _context;

    public EditModel(DrivingLicenseDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public UpdateDriverLicense? DriverLicense { get; set; } = null;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var driverLicence = await _context.DriverLicenses.FirstOrDefaultAsync(m => m.Id == id);

        if (driverLicence == null)
        {
            return NotFound();
        }

        DriverLicense = new UpdateDriverLicense
        {
            Id = driverLicence.Id,
            FirstName = driverLicence.GivenName,
            Name = driverLicence.FamilyName,
            UserName = driverLicence.UserName,
            Valid = driverLicence.Valid
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (DriverLicense != null)
        {
            var existingDriverLicense = await _context.DriverLicenses.FirstOrDefaultAsync(m => m.Id == DriverLicense.Id);

            if (existingDriverLicense == null)
                return NotFound();

            existingDriverLicense.Valid = DriverLicense.Valid;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DriverLicenceExists(DriverLicense.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./User", new { id = existingDriverLicense.UserName });
        }

        return BadRequest("No model");
    }

    private bool DriverLicenceExists(Guid id)
    {
        return _context.DriverLicenses.Any(e => e.Id == id);
    }
}
