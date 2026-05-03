using System.Text;

namespace AzureKeyVaultStudio.Helpers;


/// <summary>
/// https://github.com/DevToys-app/DevToys.Tools/blob/main/src/DevToys.Tools/Helpers/PasswordGeneratorHelper.cs
/// </summary>
internal static class PasswordGeneratorHelper
{
    /// <summary>
    /// Non-alphanumeric characters. !"#$%&')*+,-.:;=>?@]^_}~
    /// </summary>
    /// <remarks>
    /// Excluded characters. (/<[`{| and \
    /// Although it may be possible to use these characters by changing the way they are generated,
    /// they may not be suitable for use.
    /// </remarks>
    internal const string NonAlphanumeric = "!\"#$%&')*+,-.:;=>?@]^_}~";

    /// <summary>
    /// All lower case ASCII characters.
    /// </summary>
    internal const string LowercaseLetters = "abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// All upper case ASCII characters.
    /// </summary>
    internal const string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// All digits.
    /// </summary>
    internal const string Digits = "0123456789";

    internal static string GeneratePassword(
        int length,
        bool hasUppercase,
        bool hasLowercase,
        bool hasNumbers,
        bool hasSpecialCharacters,
        char[]? excludedCharacters)
    {
        if (length <= 0)
        {
            return string.Empty;
        }

        // Combine all character sets together.
        var randomCharsBuilder = new StringBuilder();
        string randomChars;

        var rand = new CryptoRandom();
        var newPasswordCharacters = new List<char>();

        if (hasUppercase)
        {
            randomCharsBuilder.Append(RemoveExcludedCharacters(UppercaseLetters, excludedCharacters));
        }

        if (hasLowercase)
        {
            randomCharsBuilder.Append(RemoveExcludedCharacters(LowercaseLetters, excludedCharacters));
        }

        if (hasNumbers)
        {
            randomCharsBuilder.Append(RemoveExcludedCharacters(Digits, excludedCharacters));
        }

        if (hasSpecialCharacters)
        {
            randomCharsBuilder.Append(RemoveExcludedCharacters(NonAlphanumeric, excludedCharacters));
        }

        randomChars = randomCharsBuilder.ToString();

        // Only continue if the user hasn't excluded everything.
        if (randomChars.Length != 0)
        {
            for (int j = 0; j < length; j++)
            {
                newPasswordCharacters.Insert(rand.Next(0, newPasswordCharacters.Count + 1), randomChars[rand.Next(0, randomChars.Length)]);
            }
        }

        return new string(newPasswordCharacters.ToArray());
    }

    private static string RemoveExcludedCharacters(string input, char[]? excludedCharacters)
    {
        if (excludedCharacters == null || excludedCharacters.Length == 0)
        {
            return input;
        }

        var excludedSet = new HashSet<char>(excludedCharacters); // HashSet provides a faster lookup than Array.Contains().
        var stringBuilder = new StringBuilder();

        foreach (char c in input)
        {
            if (!excludedSet.Contains(c))
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString();
    }
}
