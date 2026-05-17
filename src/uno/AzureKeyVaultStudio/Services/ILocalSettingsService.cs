namespace AzureKeyVaultStudio.Services;

public interface ILocalSettingsService
{
    TValue GetValue<TValue>(string key, TValue defaultValue = default!);

    void Remove(string key);

    void SetValue<TValue>(string key, TValue value);

    bool TryGetValue<TValue>(string key, out TValue value);
}
