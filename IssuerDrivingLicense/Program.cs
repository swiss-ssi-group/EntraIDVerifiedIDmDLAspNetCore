using System.Configuration;
using IssuerDrivingLicense;
using IssuerDrivingLicense.Persistence;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

var services = builder.Services;
var configuration = builder.Configuration;

services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

services.AddSecurityHeaderPolicies()
    .SetPolicySelector(ctx => SecurityHeadersDefinitions
        .GetHeaderPolicyCollection(builder.Environment.IsDevelopment()));

services.Configure<CredentialSettings>(configuration.GetSection("CredentialSettings"));
services.AddScoped<DriverLicenseService>();
services.AddScoped<IssuerService>();

services.AddDatabaseDeveloperPageExceptionFilter();
services.AddDbContext<DrivingLicenseDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection")));

services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"));

services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

services.AddDistributedMemoryCache();

services.AddRazorPages()
    .AddMvcOptions(options => { })
    .AddMicrosoftIdentityUI();

services.AddRazorPages();

var app = builder.Build();

app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
