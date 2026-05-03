namespace AzureKeyVaultStudio.Models;

public record AppConfig
{
    public string? Environment { get; init; }
}
public record ProjectUrls
{
    public string? GitHubRepoistoryBaseUrl { get; init; } = "https://github.com/cricketthomas/KeyVaultExplorer";
    public string? LicenseUrl { get; init; } = "https://github.com/cricketthomas/KeyVaultExplorer/blob/master/LICENSE";
    public string? NewIssueUrl { get; init; } = "https://github.com/cricketthomas/KeyVaultExplorer/issues/new";
    public string? ReleasesPageUrl { get; init; } = "https://github.com/cricketthomas/AzureKeyVaultExplorer/releases";
}
