using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Web;

namespace IssuerDrivingLicense;

public class CredentialSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CertificateName { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;
    public string VCServiceScope { get; set; } = "3db474b9-6a0c-4840-96ac-1fceb342124f/.default";
    public string CredentialManifest { get; set; } = string.Empty;
    public string IssuerAuthority { get; set; } = string.Empty;
    public string VerifierAuthority { get; set; } = string.Empty;
    public string VcApiCallbackApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Checks if the sample is configured for using ClientSecret or Certificate. This method is just for the sake of this sample.
    /// You won't need this verification in your production application since you will be authenticating in AAD using one mechanism only.
    /// </summary>
    /// <param name="config">Configuration from appsettings.json</param>
    /// <returns></returns>
    public bool AppUsesClientSecret(CredentialSettings config)
    {
        string clientSecretPlaceholderValue = "[Enter here a client secret for your application]";
        string certificatePlaceholderValue = "[Or instead of client secret: Enter here the name of a certificate (from the user cert store) as registered with your application]";

        if (!String.IsNullOrWhiteSpace(config.ClientSecret) && config.ClientSecret != clientSecretPlaceholderValue)
        {
            return true;
        }

        else if (!String.IsNullOrWhiteSpace(config.CertificateName) && config.CertificateName != certificatePlaceholderValue)
        {
            return false;
        }

        else
            throw new Exception("You must choose between using client secret or certificate. Please update appsettings.json file.");
    }

    public X509Certificate2? ReadCertificate(string certificateName)
    {
        if (string.IsNullOrWhiteSpace(certificateName))
        {
            throw new ArgumentException("certificateName should not be empty. Please set the CertificateName setting in the appsettings.json", nameof(certificateName));
        }

        var certificateDescription = CertificateDescription.FromStoreWithDistinguishedName(certificateName);
        var defaultCertificateLoader = new DefaultCertificateLoader();
        defaultCertificateLoader.LoadIfNeeded(certificateDescription);
        return certificateDescription?.Certificate;
    }
}