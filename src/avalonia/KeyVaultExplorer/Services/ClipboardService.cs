using Avalonia.Input.Platform;
using System;
using System.Threading.Tasks;

namespace KeyVaultExplorer.Services;

public class ClipboardService
{
    protected virtual IClipboard? Clipboard => _clipboard ??= Avalonia.Application.Current.GetTopLevel()?.Clipboard;
    private IClipboard? _clipboard;

    public Task<string?> GetTextAsync() => Clipboard?.GetTextAsync() ?? Task.FromResult<string?>(null);

    public Task SetTextAsync(string? text) => Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;

    public Task ClearAsync() => Clipboard?.ClearAsync() ?? Task.CompletedTask;

    public Task SetDataObjectAsync(Avalonia.Input.IDataObject data) => Clipboard?.SetDataObjectAsync(data) ?? Task.CompletedTask;

    public Task<string[]> GetFormatsAsync() => Clipboard?.GetFormatsAsync() ?? Task.FromResult(Array.Empty<string>());

    public Task<object?> GetDataAsync(string format) => Clipboard?.GetDataAsync(format) ?? Task.FromResult<object?>(null);
}