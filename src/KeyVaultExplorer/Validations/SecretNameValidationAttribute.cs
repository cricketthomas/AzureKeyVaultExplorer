using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace KeyVaultExplorer.Validations;

public class SecretNameValidationAttribute : ObservableValidator
{
    private static readonly Regex _validSecretNameRegex = new Regex(@"^[a-zA-Z0-9\-]+$", RegexOptions.Compiled);

    private const string ErrorMessage = "Secret names can only contain alphanumeric characters and dashes.";

    public static ValidationResult ValidateName(string value, ValidationContext context)
    {
        if (value is string secretName)
            if (_validSecretNameRegex.IsMatch(secretName))
                return ValidationResult.Success;
            else
                return new ValidationResult(ErrorMessage);

        return new ValidationResult("Invalid secret name format.");
    }
}