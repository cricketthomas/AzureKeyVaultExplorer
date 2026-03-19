using System.Runtime.CompilerServices;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultStudio.Database;
using AzureKeyVaultStudio.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace AzureKeyVaultStudio.Services;
public partial class VaultService
{
    public VaultService(AuthService authService, IMemoryCache memoryCache)
    {
        _authService = authService;
        _memoryCache = memoryCache;
    }

    private AuthService _authService { get; set; }
    private IMemoryCache _memoryCache { get; set; }

    private async ValueTask<CustomTokenCredential> GetKeyVaultCredentialAsync()
    {
        var result = await _authService.GetAzureKeyVaultTokenSilent();
        if (result is null)
            throw new AuthenticationRequiredException("No accounts logged in. Please sign in to continue.");
        return new CustomTokenCredential(result);
    }

    private async ValueTask<ArmClient> GetOrCreateArmClient()
    {
        var armClient = await _memoryCache.GetOrCreateAsync($"armclient_{_authService.TenantId}", async f =>
        {
            var authenticationResult = await _authService.GetAzureArmTokenSilent();
            if (authenticationResult is null)
                throw new AuthenticationRequiredException("No accounts logged in. Please sign in to continue.");
            f.AbsoluteExpiration = authenticationResult.ExpiresOn.AddMinutes(-5);
            var token = new CustomTokenCredential(authenticationResult);
            return new ArmClient(token);
        });

        return armClient!;
    }

    public async Task<KeyVaultKey> CreateKey(KeyVaultKey key, Uri KeyVaultUri)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new KeyClient(KeyVaultUri, token);
        return await client.CreateKeyAsync(key.Name, key.KeyType);
    }

    public async Task<KeyVaultSecret> CreateSecret(KeyVaultSecret secret, Uri KeyVaultUri)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new SecretClient(KeyVaultUri, token);
        return await client.SetSecretAsync(secret);
    }

    public async IAsyncEnumerable<SubscriptionResourceWithNextPageToken> GetAllSubscriptions([EnumeratorCancellation] CancellationToken cancellationToken = default, string? continuationToken = null)
    {
        var armClient = await GetOrCreateArmClient();
        var subscriptionsPageable = armClient.GetSubscriptions().GetAllAsync(cancellationToken).AsPages(continuationToken);

        await foreach (var subscription in subscriptionsPageable)
        {
            foreach (var subscriptionResource in subscription.Values)
            {
                yield return new SubscriptionResourceWithNextPageToken(subscriptionResource, subscription.ContinuationToken ?? "");
            }
        }
    }

    public async Task<KeyVaultCertificateWithPolicy> GetCertificate(Uri keyVaultUri, string name)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new CertificateClient(keyVaultUri, token);
        try
        {
            var response = await client.GetCertificateAsync(name);
            return response;
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            throw new KeyVaultItemNotFoundException(ex.Message, ex);
        }
    }

    public async Task<List<CertificateProperties>> GetCertificateProperties(Uri keyVaultUri, string name)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new CertificateClient(keyVaultUri, token);
        List<CertificateProperties> list = new();
        try
        {
            var response = client.GetPropertiesOfCertificateVersionsAsync(name);
            await foreach (CertificateProperties item in response)
            {
                list.Add(item);
            }
            return list;
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            throw new KeyVaultItemNotFoundException(ex.Message, ex);
        }
    }

    public async Task<KeyVaultKey> GetKey(Uri keyVaultUri, string name)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new KeyClient(keyVaultUri, token);
        try
        {
            var response = await client.GetKeyAsync(name);
            return response;
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            throw new KeyVaultItemNotFoundException(ex.Message, ex);
        }
    }

    public async Task<List<KeyProperties>> GetKeyProperties(Uri keyVaultUri, string name)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new KeyClient(keyVaultUri, token);
        List<KeyProperties> list = new();
        try
        {
            var response = client.GetPropertiesOfKeyVersionsAsync(name);
            await foreach (KeyProperties item in response)
            {
                list.Add(item);
            }
            return list;
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            throw new KeyVaultItemNotFoundException(ex.Message, ex);
        }
    }

    public async IAsyncEnumerable<KeyVaultResource> GetKeyVaultResource()
    {
        var armClient = await GetOrCreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        await foreach (var kvResource in subscription.GetKeyVaultsAsync())
        {
            yield return kvResource;
        }
    }

    public async Task<KeyVaultResource> GetKeyVaultResource(string subscriptionId, string resourceGroupName, string vaultName)
    {
        var armClient = await GetOrCreateArmClient();
        var resourceIdentifier = KeyVaultResource.CreateResourceIdentifier(subscriptionId: subscriptionId, resourceGroupName: resourceGroupName, vaultName: vaultName);
        return await armClient.GetKeyVaultResource(resourceIdentifier).GetAsync();
    }

    /// <summary>
    /// returns all key vaults based on all the subscriptions the user has rights to view.
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyList<SubscriptionResource>> GetSubscriptionsAsync()
    {
        ArmClient armClient;
        try
        {
            armClient = await GetOrCreateArmClient();
        }
        catch (AuthenticationRequiredException)
        {
            return [];
        }

        var result = await _memoryCache.GetOrCreateAsync($"subscriptions_{_authService.TenantId}", async f =>
        {
            f.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            var savedSubscriptions = await DbContext.GetStoredSubscriptions(_authService.TenantId ?? null);
            List<SubscriptionResource> subscriptionCollection = [];
            foreach (var sub in savedSubscriptions)
            {
                var sr = await armClient.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(sub.SubscriptionId)).GetAsync();
                subscriptionCollection.Add(sr.Value);
            }

            if (subscriptionCollection.Any())
                return (IEnumerable<SubscriptionResource>)subscriptionCollection;

            return armClient.GetSubscriptions().ToList();
        });

        return result?.ToList() ?? [];
    }

    public async IAsyncEnumerable<KvSubscriptionModel> GetKeyVaultResourceBySubscription()
    {
        var subscriptions = await GetSubscriptionsAsync();

        foreach (var subscription in subscriptions)
        {
            var rgPlaceholder = new KvResourceGroupModel() //needed to show chevron
            {
                KeyVaultResources = [new KeyVaultResourcePlaceholder()],
            };

            var resource = new KvSubscriptionModel
            {
                DisplayName = subscription.Data.DisplayName,
                SubscriptionId = subscription.Data.Id,
                Subscription = subscription,
                ResourceGroups = [rgPlaceholder]
            };
            yield return resource;
        }
    }

    public async IAsyncEnumerable<KeyVaultResource> GetKeyVaultResources()
    {
        var armClient = await GetOrCreateArmClient();
        ;
        foreach (var subscription in armClient.GetSubscriptions().ToArray())
        {
            await foreach (var kvResource in subscription.GetKeyVaultsAsync())
            {
                yield return kvResource;
            }
        }
    }

    public async IAsyncEnumerable<KeyVaultResource> GetKeyVaultsByResourceGroup(ResourceGroupResource resource)
    {
        var armClient = await GetOrCreateArmClient();

        await foreach (var kvResource in resource.GetKeyVaults())
        {
            yield return kvResource;
        }
    }

    public async IAsyncEnumerable<KeyVaultResource> GetKeyVaultsBySubscription(KvSubscriptionModel resource)
    {
        var armClient = await GetOrCreateArmClient();
        resource.Subscription = armClient.GetSubscriptionResource(resource.Subscription.Id);

        foreach (var kvResource in resource.Subscription.GetKeyVaults())
        {
            yield return kvResource;
        }
    }

    public async IAsyncEnumerable<ResourceGroupResource> GetResourceGroupBySubscription(KvSubscriptionModel resource)
    {
        var armClient = await GetOrCreateArmClient();
        resource.Subscription = armClient.GetSubscriptionResource(resource.Subscription.Id);

        foreach (var kvResourceGroup in resource.Subscription.GetResourceGroups())
        {
            yield return kvResourceGroup;
        }
    }

    public async Task<KeyVaultSecret> GetSecret(Uri keyVaultUri, string name, string version = null)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new SecretClient(keyVaultUri, token);
        try
        {
            var secret = await client.GetSecretAsync(name, version);
            return secret;
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            throw new KeyVaultItemNotFoundException(ex.Message, ex);
        }
        catch (Exception ex) when (ex.Message.Contains("403"))
        {
            throw new KeyVaultInsufficientPrivilegesException(ex.Message, ex);
        }
    }

    public async Task<List<SecretProperties>> GetSecretProperties(Uri keyVaultUri, string name)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new SecretClient(keyVaultUri, token);
        List<SecretProperties> list = new();
        try
        {
            var response = client.GetPropertiesOfSecretVersionsAsync(name);
            await foreach (SecretProperties item in response)
            {
                list.Add(item);
            }
            return list;
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            throw new KeyVaultItemNotFoundException(ex.Message, ex);
        }
    }

    public async Task<Dictionary<string, KeyVaultResource>> GetStoredSelectedSubscriptions(string subscriptionId)
    {
        var resource = new ResourceIdentifier(subscriptionId);
        var armClient = await GetOrCreateArmClient();
        SubscriptionResource subscription = armClient.GetSubscriptionResource(resource);

        var vaults = subscription.GetKeyVaultsAsync();
        Dictionary<string, KeyVaultResource> savedSubs = [];
        await foreach (var vault in vaults)
        {
            savedSubs.Add(resource.SubscriptionId!, vault);
        }

        return savedSubs;
    }

    public record SubscriptionResourceWithNextPageToken(SubscriptionResource SubscriptionResource, string ContinuationToken);

    public async IAsyncEnumerable<CertificateProperties> GetVaultAssociatedCertificates(Uri keyVaultUri, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new CertificateClient(keyVaultUri, token);
        await foreach (var certProperties in client.GetPropertiesOfCertificatesAsync().WithCancellation(cancellationToken))
        {
            yield return certProperties;
        }
    }

    public async IAsyncEnumerable<KeyProperties> GetVaultAssociatedKeys(Uri keyVaultUri, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new KeyClient(keyVaultUri, token);
        await foreach (var keyProperties in client.GetPropertiesOfKeysAsync().WithCancellation(cancellationToken))
        {
            yield return keyProperties;
        }
    }

    public async IAsyncEnumerable<SecretProperties> GetVaultAssociatedSecrets(Uri keyVaultUri, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (keyVaultUri is not null)
        {
            var token = await GetKeyVaultCredentialAsync();
            var client = new SecretClient(keyVaultUri, token);
            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                yield return secretProperties;
            }
        }
    }

    public static async IAsyncEnumerable<KeyVaultResource> GetWithKeyVaultsBySubscriptionAsync(KvSubscriptionModel resource)
    {
        await foreach (var kvResource in resource.Subscription.GetKeyVaultsAsync())
        {
            yield return kvResource;
        }
    }

    public async Task<KeyVaultKey> UpdateKey(KeyProperties properties, Uri KeyVaultUri)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new KeyClient(KeyVaultUri, token);
        return await client.UpdateKeyPropertiesAsync(properties);
    }

    public async Task<SecretProperties> UpdateSecret(SecretProperties properties, Uri KeyVaultUri)
    {
        var token = await GetKeyVaultCredentialAsync();
        var client = new SecretClient(KeyVaultUri, token);
        return await client.UpdateSecretPropertiesAsync(properties);
    }
}
