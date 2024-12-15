using System.Globalization;
using System.Security.Cryptography;
using IssuerDrivingLicense.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace IssuerDrivingLicense;

public class IssuerService
{
    protected readonly CredentialSettings _credentialSettings;
    protected IMemoryCache _cache;
    protected readonly ILogger<IssuerService> _log;
    private readonly DriverLicenseService _driverLicenseService;

    public IssuerService(IOptions<CredentialSettings> credentialSettings,
        IMemoryCache memoryCache,
        ILogger<IssuerService> log,
        DriverLicenseService driverLicenseService)
    {
        _credentialSettings = credentialSettings.Value;
        _credentialSettings ??= new CredentialSettings();

        _cache = memoryCache;
        _log = log;
        _driverLicenseService = driverLicenseService;
    }

    public async Task<IssuanceRequestPayload> GetIssuanceRequestPayloadAsync(HttpRequest request, HttpContext context)
    {
        var payload = new IssuanceRequestPayload();
        var length = 4;
        var pinMaxValue = (int)Math.Pow(10, length) - 1;
        var randomNumber = RandomNumberGenerator.GetInt32(1, pinMaxValue);
        var newpin = string.Format(CultureInfo.InvariantCulture,
            "{0:D" + length.ToString(CultureInfo.InvariantCulture) + "}", randomNumber);

        payload.Pin.Length = length;
        payload.Pin.Value = newpin;
        payload.CredentialsType = "Iso18013DriversLicense";
        payload.Manifest = _credentialSettings.CredentialManifest;

        var host = GetRequestHostName(request);
        payload.Callback.State = Guid.NewGuid().ToString();
        payload.Callback.Url = $"{host}/api/issuer/issuanceCallback";
        payload.Callback.Headers.ApiKey = _credentialSettings.VcApiCallbackApiKey;

        payload.Registration.ClientName = "NDL Iso18013 DriversLicense";
        payload.Authority = _credentialSettings.IssuerAuthority;

        var driverLicense = await _driverLicenseService.GetDriverLicense(context.User?.Identity?.Name);

        // TODO complete fields
        payload.Claims.FamilyName = driverLicense!.FamilyName;
        payload.Claims.GivenName = driverLicense!.GivenName;
        payload.Claims.BirthDate = $"{driverLicense!.DateOfBirth:yyyy-MM-dd}";
        payload.Claims.IssueDate = $"{driverLicense!.IssueDate.ToString("s")}";
        payload.Claims.ExpiryDate = $"{driverLicense!.ExpiryDate.ToString("s")}";
        // 2 code, defined in ISO 3166-1
        payload.Claims.IssuingCountry = driverLicense!.IssuingCountry;
        payload.Claims.IssuingAuthority = driverLicense!.IssuingAuthority;
        payload.Claims.DocumentNumber = driverLicense!.DocumentNumber;
        payload.Claims.AdministrativeNumber = driverLicense!.AdministrativeNumber;
        //{
        //  "codes": [{ "code": "D"}],
        //  "vehicle_category_code": "D",
        //  "issue_date": "2019-01-01",
        //  "expiry_date": "2027-01-01"
        //}
        payload.Claims.DrivingPrivileges = driverLicense!.DrivingPrivileges;
        // Distinguishing sign of the issuing country according to 18013-1 annex F NOTE this field is added for purposes of the UN conventions on driving licences
        payload.Claims.UnDistinguishingSign = driverLicense!.UnDistinguishingSign;

        return payload;
    }

    public async Task<(string Token, string Error, string ErrorDescription)> GetAccessToken()
    {
        // You can run this sample using ClientSecret or Certificate. The code will differ only when instantiating the IConfidentialClientApplication
        var isUsingClientSecret = _credentialSettings.AppUsesClientSecret(_credentialSettings);

        // Since we are using application permissions this will be a confidential client application
        IConfidentialClientApplication app;
        if (isUsingClientSecret)
        {
            app = ConfidentialClientApplicationBuilder.Create(_credentialSettings.ClientId)
                .WithClientSecret(_credentialSettings.ClientSecret)
                .WithAuthority(new Uri(_credentialSettings.Authority))
                .Build();
        }
        else
        {
            var certificate = _credentialSettings.ReadCertificate(_credentialSettings.CertificateName);
            app = ConfidentialClientApplicationBuilder.Create(_credentialSettings.ClientId)
                .WithCertificate(certificate)
                .WithAuthority(new Uri(_credentialSettings.Authority))
                .Build();
        }

        //configure in memory cache for the access tokens. The tokens are typically valid for 60 seconds,
        //so no need to create new ones for every web request
        app.AddDistributedTokenCache(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddLogging(configure => configure.AddConsole())
            .Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Debug);
        });

        // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
        // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
        // a tenant administrator. 
        var scopes = new string[] { _credentialSettings.VCServiceScope };

        AuthenticationResult? result = null;
        try
        {
            result = await app.AcquireTokenForClient(scopes)
                .ExecuteAsync();
        }
        catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
        {
            // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
            // Mitigation: change the scope to be as expected
            return (string.Empty, "500", "Scope provided is not supported");
            //return BadRequest(new { error = "500", error_description = "Scope provided is not supported" });
        }
        catch (MsalServiceException ex)
        {
            // general error getting an access token
            return (string.Empty, "500", "Something went wrong getting an access token for the client API:" + ex.Message);
            //return BadRequest(new { error = "500", error_description = "Something went wrong getting an access token for the client API:" + ex.Message });
        }

        _log.LogTrace("{AccessToken}", result.AccessToken);
        return (result.AccessToken, string.Empty, string.Empty);
    }

    public string GetRequestHostName(HttpRequest request)
    {
        var scheme = "https";// : this.Request.Scheme;
        var originalHost = request.Headers["x-original-host"];
        if (!string.IsNullOrEmpty(originalHost))
        {
            return $"{scheme}://{originalHost}";
        }
        else
        {
            return $"{scheme}://{request.Host}";
        }
    }
}
