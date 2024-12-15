using System.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using VerifierInsuranceCompany;

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

services.AddScoped<VerifierService>();
services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

services.Configure<CredentialSettings>(configuration.GetSection("CredentialSettings"));
services.AddHttpClient();
services.AddDistributedMemoryCache();

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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapRazorPages();

app.Run();
