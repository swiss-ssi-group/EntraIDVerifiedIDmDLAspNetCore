using IssuerDrivingLicense.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IssuerDrivingLicense.Pages.DriverLicenses;

public class UserModel : PageModel
{
    private readonly DrivingLicenseDbContext _context;

    [FromQuery(Name = "id")]
    public string? UserName { get; set; }

    public UserModel(DrivingLicenseDbContext context)
    {
        _context = context;
    }

    public IList<DriverLicense> DriverLicense { get; set; } = new List<DriverLicense>();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (id == null)
        {
            return NotFound();
        }
        UserName = id;

        DriverLicense = await _context.DriverLicenses
            .AsQueryable()
            .Where(item => item.UserName == id)
            .ToListAsync();

        return Page();
    }
}
