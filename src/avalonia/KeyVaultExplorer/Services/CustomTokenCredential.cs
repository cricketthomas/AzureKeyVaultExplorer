using Azure.Core;
using Microsoft.Identity.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyVaultExplorer.Services;

public class CustomTokenCredential : TokenCredential
{
    private readonly string _token;
    private readonly DateTimeOffset _expiresOn;

    public CustomTokenCredential(AuthenticationResult accessResult)
    {
        _expiresOn = accessResult.ExpiresOn;
        _token = accessResult.AccessToken;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(_token, _expiresOn);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new AccessToken(_token, _expiresOn));
    }
}