using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using AzureKeyVaultStudio.Helpers;


namespace AzureKeyVaultStudio.Presentation.ViewModels;

public partial class PasswordGeneratorViewModel : ObservableValidator
{
    [ObservableProperty]
    [Range(1, 512, ErrorMessage = "Length must be between 1 and 512 characters.")]
    public partial int Length { get; set; } = 32;

    [ObservableProperty]
    public partial bool IncludeLowercase { get; set; } = true;

    [ObservableProperty]
    public partial bool IncludeUppercase { get; set; } = true;

    [ObservableProperty]
    public partial bool IncludeNumbers { get; set; } = true;

    [ObservableProperty]
    public partial bool IncludeSpecialCharacters { get; set; } = true;

    [ObservableProperty]
    public partial string? ExcludedCharacters { get; set; }

    [ObservableProperty]
    public partial string? Password { get; set; }

    [RelayCommand]
    private void Generate()
    {
        var excluded = string.IsNullOrWhiteSpace(ExcludedCharacters)
            ? null
            : ExcludedCharacters
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(s => s.Trim())
                .Distinct()
                .ToArray();

        Password = PasswordGeneratorHelper.GeneratePassword(
            length: Length,
            hasUppercase: IncludeUppercase,
            hasLowercase: IncludeLowercase,
            hasNumbers: IncludeNumbers,
            hasSpecialCharacters: IncludeSpecialCharacters,
            excludedCharacters: excluded);
    }





    [RelayCommand]
    private void TestValidateCommand()
    {
        try
        {
            this.ValidateAllProperties();

            this.ValidateProperty(this.Length, "Length");
        }
        finally
        {

        }
        if (HasErrors)
        {
            Debug.WriteLine("Has errors");
        }

    }




}
