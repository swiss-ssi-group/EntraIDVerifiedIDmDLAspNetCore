using IssuerDrivingLicense.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IssuerDrivingLicense;

public class AdminModel : PageModel
{
    private readonly DrivingLicenseDbContext _context;

    public AdminModel(DrivingLicenseDbContext context)
    {
        _context = context;
    }

    public List<DriverLicense> DriverLicenses { get; set; } = new List<DriverLicense>();

    public async Task OnGetAsync()
    {
        DriverLicenses = await _context.DriverLicenses.ToListAsync();
    }
}
