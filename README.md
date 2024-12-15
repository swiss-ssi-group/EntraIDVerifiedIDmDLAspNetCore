
# ASP.NET Core Issue and Verify Verifiable Credentials using Microsoft Entra Verified ID 

[![.NET](https://github.com/swiss-ssi-group/EntraIDVerifiedIDmDLAspNetCore/actions/workflows/dotnet.yml/badge.svg)](https://github.com/swiss-ssi-group/EntraIDVerifiedIDmDLAspNetCore/actions/workflows/dotnet.yml)

## Blogs

- [Getting started with Self Sovereign Identity SSI](https://damienbod.com/2021/03/29/getting-started-with-self-sovereign-identity-ssi/)
- [Challenges to Self Sovereign Identity](https://damienbod.com/2021/10/11/challenges-to-self-sovereign-identity/)
- [Create and issue verifiable credentials in ASP.NET Core using Azure AD](https://damienbod.com/2021/10/25/create-and-issuer-verifiable-credentials-in-asp-net-core-using-azure-ad/)

## History

- 2024-12-15 .NET 9, Updated packages
- 2023-07-29 Updated packages
- 2023-06-24 Updated VC, using mDL based driving license
- 2023-06-23 Update subject model, based on https://github.com/w3c-ccg/vdl-vocab/blob/main/context/v1.jsonld
- 2023-06-18 Updated packages
- 2023-04-28 Updated packages
- 2023-03-05 Fixed new VC payloads, fixed cache, recreated all VCs, added github actions for Azure deployment
- 2023-03-03 Updated to .NET 7, Update AAD VC service with all the breaking changes
- 2022-03-18 Updated code 
- 2021-11-12 Updated to .NET 6 release

## User secrets and issuer/verify configuration

Select the correct endpoint depending to the business of the application.

```
{
  "CredentialSettings": {
    "Endpoint": "https://verifiedid.did.msidentity.com/v1.0/verifiableCredentials/createPresentationRequest",
    //  "Endpoint": "https://verifiedid.did.msidentity.com/v1.0/verifiableCredentials/createIssuanceRequest",
    "VCServiceScope": "bbb94529-53a3-4be5-a069-7eaf2712b826/.default",
    "Instance": "https://login.microsoftonline.com/{0}",
    "TenantId": "YOURTENANTID",
    "ClientId": "APPLICATION CLIENT ID",
    "VcApiCallbackApiKey": "SECRET",
    "Authority": "YOUR authority",
    "ClientSecret": "[client secret or instead use the prefered certificate in the next entry]",
    // "CertificateName": "[Or instead of client secret: Enter here the name of a certificate (from the user cert store) as registered with your application]",
    "IssuerAuthority": "YOUR VC SERVICE DID",
    "VerifierAuthority": "YOUR VC SERVICE DID",
    "CredentialManifest":  "THE CREDENTIAL URL FROM THE VC PORTAL"
  }
}
```


## Iso18013 Drivers License (mDL) scheme

I would like to base this on a standard scheme, but could not find any.

Found 2 drafts:

https://github.com/w3c-ccg/vdl-vocab/blob/main/context/v1.jsonld

https://w3c-ccg.github.io/vdl-vocab/

## Local debugging, required for callback

```
ngrok http https://localhost:5001
```

## Feedback in issuer app

When running the issuer application, ngrok is used if you would like to receive feedback from the VC issuing through the callback. This requires a public IP. The IP needs to be added to the **Azure App Registration** as a redirect URL to authenticate. ngrok is only used for development.

## CredentialsClaims

The **CredentialsClaims** classes in both the issuer and the verifier are used to add the specific claims to the definition of the Azure VC credential.

The classes must match the definitions. By changing these classes and the rules fields, different Azure verifiable credentials can be used.

## GetIssuanceRequestPayloadAsync

This method defines the specifics of the issue request payload. This would need to be changed, if you required no pin verification or other flows. See the Azure AD VC docs for more info.

## Microsoft sample APP 

[sample](https://github.com/Azure-Samples/active-directory-verifiable-credentials-dotnet)

## Database

```
Add-Migration "init"

Update-Database
```
## Links

https://docs.microsoft.com/en-us/azure/active-directory/verifiable-credentials/

https://docs.microsoft.com/en-us/azure/active-directory/verifiable-credentials/get-started-request-api

https://github.com/Azure-Samples/active-directory-verifiable-credentials-dotnet

https://www.microsoft.com/de-ch/security/business/identity-access-management/decentralized-identity-blockchain

https://didproject.azurewebsites.net/docs/issuer-setup.html

https://didproject.azurewebsites.net/docs/credential-design.html

https://github.com/Azure-Samples/active-directory-verifiable-credentials

https://identity.foundation/

https://www.w3.org/TR/vc-data-model/

https://daniel-krzyczkowski.github.io/Azure-AD-Verifiable-Credentials-Intro/

https://dotnetthoughts.net/using-node-services-in-aspnet-core/

https://identity.foundation/ion/explorer

https://www.npmjs.com/package/ngrok

https://github.com/microsoft/VerifiableCredentials-Verification-SDK-Typescript

https://identity.foundation/ion/explorer

https://www.npmjs.com/package/ngrok

https://github.com/microsoft/VerifiableCredentials-Verification-SDK-Typescript
