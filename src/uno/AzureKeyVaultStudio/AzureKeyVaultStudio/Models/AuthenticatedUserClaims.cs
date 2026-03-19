namespace AzureKeyVaultStudio.Models;


[Microsoft.UI.Xaml.Data.Bindable]

public class AuthenticatedUserClaims
{
    public string Username { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Name { get; set; } = string.Empty;
}
