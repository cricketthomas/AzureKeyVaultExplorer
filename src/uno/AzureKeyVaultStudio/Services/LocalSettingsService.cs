using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AzureKeyVaultStudio.Services;

public static class LocalSettingsServiceFactory
{
    public static ILocalSettingsService Create()
    {
        if (Constants.IsAppPackaged)
        {
            return new ApplicationDataLocalSettingsService(ApplicationData.Current.LocalSettings);
        }
        else
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.AppName);
            return new FileLocalSettingsService(Path.Combine(folder, "localsettings.json"));
        }
    }

}

public static class LocalSettingsServiceProvider
{
    private static ILocalSettingsService? _instance;

    public static ILocalSettingsService Instance => _instance ?? throw new InvalidOperationException("Local settings service has not been initialized.");

    public static void Initialize(ILocalSettingsService service)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        _instance ??= service;
    }
}

internal sealed  class ApplicationDataLocalSettingsService : ILocalSettingsService
{
    private readonly ApplicationDataContainer _container;

    public ApplicationDataLocalSettingsService(ApplicationDataContainer container)
    {
        _container = container;
    }

    public TValue GetValue<TValue>(string key, TValue defaultValue = default!)
    {
        return TryGetValue(key, out TValue value) ? value : defaultValue;
    }

    public void Remove(string key)
    {
        _container.Values.Remove(key);
    }

    public void SetValue<TValue>(string key, TValue value)
    {
        if (value is null)
        {
            _container.Values.Remove(key);
            return;
        }

        _container.Values[key] = value!;
    }

    public bool TryGetValue<TValue>(string key, out TValue value)
    {
        if (!_container.Values.TryGetValue(key, out var stored))
        {
            value = default!;
            return false;
        }

        if (stored is TValue typed)
        {
            value = typed;
            return true;
        }

        try
        {
            if (stored is string str && typeof(TValue) == typeof(string))
            {
                value = (TValue)(object)str;
                return true;
            }

            value = (TValue)Convert.ChangeType(stored, typeof(TValue));
            return true;
        }
        catch
        {
            value = default!;
            return false;
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(decimal))]
internal partial class LocalSettingsJsonContext : JsonSerializerContext
{
    public static JsonTypeInfo<TValue>? TryGetTypeInfo<TValue>()
    {
        if (typeof(TValue) == typeof(string)) return (JsonTypeInfo<TValue>)(object)Default.String;
        if (typeof(TValue) == typeof(bool)) return (JsonTypeInfo<TValue>)(object)Default.Boolean;
        if (typeof(TValue) == typeof(int)) return (JsonTypeInfo<TValue>)(object)Default.Int32;
        if (typeof(TValue) == typeof(long)) return (JsonTypeInfo<TValue>)(object)Default.Int64;
        if (typeof(TValue) == typeof(double)) return (JsonTypeInfo<TValue>)(object)Default.Double;
        if (typeof(TValue) == typeof(float)) return (JsonTypeInfo<TValue>)(object)Default.Single;
        if (typeof(TValue) == typeof(decimal)) return (JsonTypeInfo<TValue>)(object)Default.Decimal;
        return null;
    }
}

internal sealed class FileLocalSettingsService : ILocalSettingsService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private readonly Dictionary<string, JsonElement> _values;

    public FileLocalSettingsService(string filePath)
    {
        _filePath = filePath;
        _values = LoadValues(filePath);
    }

    public TValue GetValue<TValue>(string key, TValue defaultValue = default!)
    {
        return TryGetValue(key, out TValue value) ? value : defaultValue;
    }

    public void Remove(string key)
    {
        SetValue<object?>(key, null);
    }

    public void SetValue<TValue>(string key, TValue value)
    {
        lock (_lock)
        {
            if (value is null)
            {
                if (_values.Remove(key))
                    PersistValues();
                return;
            }

            if (typeof(TValue) == typeof(CultureInfo))
            {
                var cultureName = ((CultureInfo)(object)value).Name;
                _values[key] = JsonSerializer.SerializeToElement(cultureName, LocalSettingsJsonContext.Default.String);
                PersistValues();
                return;
            }

            if (typeof(TValue).IsEnum)
            {
                _values[key] = JsonSerializer.SerializeToElement(value.ToString(), LocalSettingsJsonContext.Default.String);
                PersistValues();
                return;
            }

            var typeInfo = LocalSettingsJsonContext.TryGetTypeInfo<TValue>()
                ?? throw new NotSupportedException($"Type '{typeof(TValue)}' is not supported by LocalSettings source-gen.");

            _values[key] = JsonSerializer.SerializeToElement(value, typeInfo);
            PersistValues();
        }
    }

    public bool TryGetValue<TValue>(string key, out TValue value)
    {
        lock (_lock)
        {
            if (!_values.TryGetValue(key, out var element))
            {
                value = default!;
                return false;
            }

            try
            {
                if (typeof(TValue) == typeof(CultureInfo))
                {
                    var name = JsonSerializer.Deserialize(element, LocalSettingsJsonContext.Default.String);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        value = default!;
                        return false;
                    }

                    value = (TValue)(object)new CultureInfo(name);
                    return true;
                }

                if (typeof(TValue).IsEnum)
                {
                    if (element.ValueKind == JsonValueKind.String &&
                        Enum.TryParse(typeof(TValue), element.GetString(), true, out var parsed))
                    {
                        value = (TValue)parsed;
                        return true;
                    }

                    if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var n))
                    {
                        value = (TValue)Enum.ToObject(typeof(TValue), n);
                        return true;
                    }

                    value = default!;
                    return false;
                }

                var typeInfo = LocalSettingsJsonContext.TryGetTypeInfo<TValue>();
                if (typeInfo is null)
                {
                    value = default!;
                    return false;
                }

                value = JsonSerializer.Deserialize(element, typeInfo)!;
                return true;
            }
            catch
            {
                value = default!;
                return false;
            }
        }
    }

    private Dictionary<string, JsonElement> LoadValues(string filePath)
    {
        if (!File.Exists(filePath))
            return new(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(filePath);
            using var document = JsonDocument.Parse(json);
            return document.RootElement.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void PersistValues()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(
            _values,
            LocalSettingsJsonContext.Default.DictionaryStringJsonElement);

        File.WriteAllText(_filePath, json);
    }

  
}
