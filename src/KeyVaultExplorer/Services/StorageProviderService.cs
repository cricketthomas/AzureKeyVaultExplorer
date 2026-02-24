using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeyVaultExplorer.Services;

public class StorageProviderService
{
    protected virtual IStorageProvider? StorageProvider => _storageProvider ??= Avalonia.Application.Current.GetTopLevel()?.StorageProvider;
    private IStorageProvider? _storageProvider;

    public bool CanOpen => StorageProvider?.CanOpen ?? false;

    public bool CanSave => StorageProvider?.CanSave ?? false;

    public bool CanPickFolder => StorageProvider?.CanPickFolder ?? false;

    public Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        return StorageProvider?.OpenFilePickerAsync(options) ?? Task.FromResult<IReadOnlyList<IStorageFile>>(Array.Empty<IStorageFile>());
    }

    public Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        return StorageProvider?.SaveFilePickerAsync(options) ?? Task.FromResult<IStorageFile?>(null);
    }

    public Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return StorageProvider?.OpenFolderPickerAsync(options) ?? Task.FromResult<IReadOnlyList<IStorageFolder>>(Array.Empty<IStorageFolder>());
    }

    public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        return StorageProvider?.OpenFileBookmarkAsync(bookmark) ?? Task.FromResult<IStorageBookmarkFile?>(null);
    }

    public Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        return StorageProvider?.OpenFolderBookmarkAsync(bookmark) ?? Task.FromResult<IStorageBookmarkFolder?>(null);
    }

    public Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
    {
        return StorageProvider?.TryGetFileFromPathAsync(filePath) ?? Task.FromResult<IStorageFile?>(null);
    }

    public Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
    {
        return StorageProvider?.TryGetFolderFromPathAsync(folderPath) ?? Task.FromResult<IStorageFolder?>(null);
    }

    public Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
    {
        return StorageProvider?.TryGetWellKnownFolderAsync(wellKnownFolder) ?? Task.FromResult<IStorageFolder?>(null);
    }
}