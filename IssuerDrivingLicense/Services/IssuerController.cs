using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using IssuerDrivingLicense.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace IssuerDrivingLicense;

[Route("api/[controller]/[action]")]
[ApiController]
public class IssuerController : ControllerBase
{
    protected readonly CredentialSettings _credentialSettings;
    protected IDistributedCache _distributedCache;
    protected readonly ILogger<IssuerController> _log;
    private readonly IssuerService _issuerService;
    private readonly HttpClient _httpClient;

    public IssuerController(IOptions<CredentialSettings> credentialSettings,
        IDistributedCache distributedCache,
        ILogger<IssuerController> log,
        IssuerService issuerService,
        IHttpClientFactory httpClientFactory)
    {
        _credentialSettings = credentialSettings.Value;
        _distributedCache = distributedCache;
        _log = log;
        _issuerService = issuerService;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// This method is called from the UI to initiate the issuance of the verifiable credential
    /// </summary>
    /// <returns>JSON object with the address to the presentation request and optionally a QR code and a state value which can be used to check on the response status</returns>
    [HttpGet("/api/issuer/issuance-request")]
    public async Task<ActionResult> IssuanceRequestAsync()
    {
        try
        {
            var payload = await _issuerService.GetIssuanceRequestPayloadAsync(Request, HttpContext);
            try
            {
                var (Token, Error, ErrorDescription) = await _issuerService.GetAccessToken();
                if (string.IsNullOrEmpty(Token))
                {
                    _log.LogError("failed to acquire accesstoken: {Error} : {ErrorDescription}", Error, ErrorDescription);
                    return BadRequest(new { error = Error, error_description = ErrorDescription });
                }

                var defaultRequestHeaders = _httpClient.DefaultRequestHeaders;
                defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

                var res = await _httpClient.PostAsJsonAsync(_credentialSettings.Endpoint, payload);

                var response = await res.Content.ReadFromJsonAsync<IssuanceResponse>();

                if (response == null)
                {
                    return BadRequest(new { error = "400", error_description = "no response from VC API" });
                }

                if (res.StatusCode == HttpStatusCode.Created)
                {
                    _log.LogTrace("succesfully called Request API");

                    if (payload.Pin.Value != null)
                    {
                        response.Pin = payload.Pin.Value;
                    }

                    response.Id = payload.Callback.State;

                    var cacheData = new CacheData
                    {
                        Status = IssuanceConst.NotScanned,
                        Message = "Request ready, please scan with Authenticator",
                        Expiry = response.Expiry.ToString(CultureInfo.InvariantCulture)
                    };
                    CacheData.AddToCache(payload.Callback.State, _distributedCache, cacheData);

                    return Ok(response);
                }
                else
                {
                    var message = await res.Content.ReadAsStringAsync();

                    _log.LogError("Unsuccesfully called Request API {message}", message);
                    return BadRequest(new { error = "400", error_description = "Something went wrong calling the API: " + response });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "400", error_description = "Something went wrong calling the API: " + ex.Message });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }
    }

    /// <summary>
    /// This method is called by the VC Request API when the user scans a QR code and accepts the issued Verifiable Credential
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/api/issuer/issuanceCallback")]
    public async Task<ActionResult> IssuanceCallback()
    {
        var content = await new StreamReader(Request.Body).ReadToEndAsync();
        var issuanceResponse = JsonSerializer.Deserialize<IssuanceCallbackResponse>(content);

        try
        {
            //there are 2 different callbacks. 1 if the QR code is scanned (or deeplink has been followed)
            //Scanning the QR code makes Authenticator download the specific request from the server
            //the request will be deleted from the server immediately.
            //That's why it is so important to capture this callback and relay this to the UI so the UI can hide
            //the QR code to prevent the user from scanning it twice (resulting in an error since the request is already deleted)
            if (issuanceResponse?.RequestStatus == IssuanceConst.RequestRetrieved)
            {
                var cacheData = new CacheData
                {
                    Status = IssuanceConst.RequestRetrieved,
                    Message = "QR Code is scanned. Waiting for issuance...",
                };
                CacheData.AddToCache(issuanceResponse.State, _distributedCache, cacheData);
            }

            if (issuanceResponse?.RequestStatus == IssuanceConst.IssuanceSuccessful)
            {
                var cacheData = new CacheData
                {
                    Status = IssuanceConst.IssuanceSuccessful,
                    Message = "Credential successfully issued",
                };
                CacheData.AddToCache(issuanceResponse.State, _distributedCache, cacheData);
            }

            if (issuanceResponse?.RequestStatus == IssuanceConst.IssuanceError)
            {
                var cacheData = new CacheData
                {
                    Status = IssuanceConst.IssuanceError,
                    Payload = issuanceResponse.Error?.Code,
                    //at the moment there isn't a specific error for incorrect entry of a pincode.
                    //So assume this error happens when the users entered the incorrect pincode and ask to try again.
                    Message = issuanceResponse.Error?.Message
                };
                CacheData.AddToCache(issuanceResponse.State, _distributedCache, cacheData);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }
    }

    /// <summary>
    /// this function is called from the UI polling for a response from the AAD VC Service.
    /// when a callback is recieved at the issuanceCallback service the session will be updated
    /// this method will respond with the status so the UI can reflect if the QR code was scanned and with the result of the issuance process
    /// </summary>
    [HttpGet("/api/issuer/issuance-response")]
    public ActionResult IssuanceResponse()
    {
        try
        {
            //the id is the state value initially created when the issuanc request was requested from the request API
            //the in-memory database uses this as key to get and store the state of the process so the UI can be updated
            string? state = Request.Query["id"];
            if (state == null)
            {
                return BadRequest(new { error = "400", error_description = "Missing argument 'id'" });
            }

            var data = CacheData.GetFromCache(state, _distributedCache);
            if (data != null)
            {
                Debug.WriteLine("check if there was a response yet: " + data);
                return new ContentResult
                {
                    ContentType = "application/json",
                    Content = JsonSerializer.Serialize(data)
                };
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }
    }
}
