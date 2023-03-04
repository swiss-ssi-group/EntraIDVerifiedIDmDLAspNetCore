﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using VerifierInsuranceCompany.Services;
using System.Text.Json;

namespace VerifierInsuranceCompany;

[Route("api/[controller]/[action]")]
public class VerifierController : Controller
{
    protected readonly CredentialSettings _credentialSettings;
    protected IMemoryCache _cache;
    protected readonly ILogger<VerifierController> _log;
    private readonly VerifierService _verifierService;
    private readonly HttpClient _httpClient;

    public VerifierController(IOptions<CredentialSettings> appSettings,
        IMemoryCache memoryCache,
        ILogger<VerifierController> log,
        VerifierService verifierService,
        IHttpClientFactory httpClientFactory)
    {
        this._credentialSettings = appSettings.Value;
        _cache = memoryCache;
        _log = log;
        _verifierService = verifierService;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// This method is called from the UI to initiate the presentation of the verifiable credential
    /// </summary>
    /// <returns>JSON object with the address to the presentation request and optionally a QR code and a state value which can be used to check on the response status</returns>
    [HttpGet("/api/verifier/presentation-request")]
    public async Task<ActionResult> PresentationRequest()
    {
        try
        {
            var payload = _verifierService.GetVerifierRequestPayload(Request);

            //The VC Request API is an authenticated API. We need to clientid and secret (or certificate) to create an access token which 
            //needs to be send as bearer to the VC Request API
            var (Token, Error, ErrorDescription) = await _verifierService.GetAccessToken();

            if (string.IsNullOrEmpty(Token))
            {
                _log.LogError("failed to acquire accesstoken: {Error} : {ErrorDescription}", Error, ErrorDescription);
                return BadRequest(new { error = Error, error_description = ErrorDescription });
            }

            var defaultRequestHeaders = _httpClient.DefaultRequestHeaders;
            defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

            HttpResponseMessage res = await _httpClient.PostAsJsonAsync(
                _credentialSettings.Endpoint, payload);

            if (res.IsSuccessStatusCode)
            {
                var response = await res.Content.ReadFromJsonAsync<VerifierResponse>();
                response.Id = payload.Callback.State;
                _log.LogTrace("succesfully called Request API");

                if (res.StatusCode == HttpStatusCode.Created)
                {
                    var cacheData = new CacheData
                    {
                        Status = VerifierConst.NotScanned,
                        Message = "Request ready, please scan with Authenticator",
                        Expiry = response.Expiry.ToString(),
                    };
                    _cache.Set(payload.Callback.State, JsonSerializer.Serialize(cacheData));

                    //the response from the VC Request API call is returned to the caller (the UI). It contains the URI to the request which Authenticator can download after
                    //it has scanned the QR code. If the payload requested the VC Request service to create the QR code that is returned as well
                    //the javascript in the UI will use that QR code to display it on the screen to the user.

                    return Ok(response);
                }
            }

            var errorResponse = await res.Content.ReadAsStringAsync();
            _log.LogError("Unsuccesfully called Request API");
            return BadRequest(new { error = "400", error_description = "Something went wrong calling the API: " + errorResponse });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }
    }

    /// <summary>
    /// This method is called by the VC Request API when the user scans a QR code and presents a Verifiable Credential to the service
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult> PresentationCallback()
    {
        string content = await new System.IO.StreamReader(Request.Body).ReadToEndAsync();
        VerifierCallbackResponse verifierCallbackResponse = JsonSerializer.Deserialize<VerifierCallbackResponse>(content);

        try
        {
            //there are 2 different callbacks. 1 if the QR code is scanned (or deeplink has been followed)
            //Scanning the QR code makes Authenticator download the specific request from the server
            //the request will be deleted from the server immediately.
            //That's why it is so important to capture this callback and relay this to the UI so the UI can hide
            //the QR code to prevent the user from scanning it twice (resulting in an error since the request is already deleted)
            if (verifierCallbackResponse.Code == VerifierConst.RequestRetrieved)
            {
                var cacheData = new CacheData
                {
                    Status = VerifierConst.RequestRetrieved,
                    Message = "QR Code is scanned. Waiting for validation...",
                };
                _cache.Set(verifierCallbackResponse.State, JsonSerializer.Serialize(cacheData));
            }

            // the 2nd callback is the result with the verified credential being verified.
            // typically here is where the business logic is written to determine what to do with the result
            // the response in this callback contains the claims from the Verifiable Credential(s) being presented by the user
            // In this case the result is put in the in memory cache which is used by the UI when polling for the state so the UI can be updated.
            if (verifierCallbackResponse.Code == VerifierConst.PresentationVerified)
            {
                var cacheData = new CacheData
                {
                    Status = VerifierConst.PresentationVerified,
                    Message = "Presentation verified",
                    Payload = JsonSerializer.Serialize(verifierCallbackResponse.Issuers),
                    Subject = verifierCallbackResponse.Subject,
                    Name = verifierCallbackResponse.Issuers?.FirstOrDefault()?.Claims.Name,
                    Details = verifierCallbackResponse.Issuers?.FirstOrDefault()?.Claims.Details

                };
                _cache.Set(verifierCallbackResponse.State, JsonSerializer.Serialize(cacheData));
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }
    }
    //
    //this function is called from the UI polling for a response from the AAD VC Service.
    //when a callback is recieved at the presentationCallback service the session will be updated
    //this method will respond with the status so the UI can reflect if the QR code was scanned and with the result of the presentation
    //
    [HttpGet("/api/verifier/presentation-response")]
    public ActionResult PresentationResponse()
    {
        try
        {
            //the id is the state value initially created when the issuanc request was requested from the request API
            //the in-memory database uses this as key to get and store the state of the process so the UI can be updated
            string state = this.Request.Query["id"];
            if (string.IsNullOrEmpty(state))
            {
                return BadRequest(new { error = "400", error_description = "Missing argument 'id'" });
            }
            CacheData value = null;
            if (_cache.TryGetValue(state, out string buf))
            {
                value = JsonSerializer.Deserialize<CacheData>(buf);

                Debug.WriteLine("check if there was a response yet: " + value);
                return new ContentResult { ContentType = "application/json", Content = JsonSerializer.Serialize(value) };
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }
    }
}
