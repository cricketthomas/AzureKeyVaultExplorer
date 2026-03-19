using DeviceId;
using KeyVaultExplorer.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KeyVaultExplorer.Services;

public static class DatabaseEncryptedPasswordManager
{
    public static async Task<string> GetSecret()
    {
        if (OperatingSystem.IsWindows())
        {
            byte[] storedProtectedKey = GetProtectedKey();
            byte[] entropySource = GetMachineEntropy();
            byte[] unprotectedKey = ProtectedData.Unprotect(storedProtectedKey, entropySource, DataProtectionScope.LocalMachine);

            string encryptedSecretPath = Path.Combine(Constants.LocalAppDataFolder, Constants.EncryptedSecretFileName);
            if (!File.Exists(encryptedSecretPath))
                return null;

            byte[] combinedData = File.ReadAllBytes(encryptedSecretPath); // Read bytes directly

            using var aes = Aes.Create();
            aes.Key = unprotectedKey;
            aes.Padding = PaddingMode.PKCS7;

            // Extract the IV and the encrypted data
            byte[] iv = new byte[aes.IV.Length];
            Array.Copy(combinedData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            byte[] encryptedSecret = new byte[combinedData.Length - iv.Length];
            Array.Copy(combinedData, iv.Length, encryptedSecret, 0, encryptedSecret.Length);

            var decryptor = aes.CreateDecryptor();
            byte[] decryptedSecretBytes = decryptor.TransformFinalBlock(encryptedSecret, 0, encryptedSecret.Length);

            return Encoding.UTF8.GetString(decryptedSecretBytes); // Decode bytes consistently
        }
        if (OperatingSystem.IsMacOS())
        {
            string password = await MacOSKeyChainService.GetPasswordAsync(Constants.KeychainSecretName, Constants.KeychainServiceName);
            return password;
        }

        // if linux then use an empty string as the password
        return "";
    }

    public static void SetSecret(string secret)
    {
        if (OperatingSystem.IsMacOS())
        {
            // HACK: is to have a file there rather than checking the keychain on mac.
            string protectedKeyPath = Path.Combine(Constants.LocalAppDataFolder, Constants.ProtectedKeyFileName.Replace("bin", "txt"));
            var dbPassExists = File.Exists(protectedKeyPath);
            if (!dbPassExists)
            {
                File.WriteAllBytes(protectedKeyPath, new byte[1]);
                MacOSKeyChainService.SetPasswordAsync(Constants.KeychainSecretName, Constants.KeychainServiceName, secret);
            }
        }

        //#if WINDOWS
        //#endif
        //#if MACOS
        //#endif

        if (OperatingSystem.IsWindows())
        {
            byte[] protectedKey = GetProtectedKey();
            byte[] entropySource = GetMachineEntropy();
            byte[] unprotectedKey = ProtectedData.Unprotect(protectedKey, entropySource, DataProtectionScope.LocalMachine);

            using (var aes = Aes.Create())
            {
                aes.Key = unprotectedKey;
                aes.Padding = PaddingMode.PKCS7;

                var encryptor = aes.CreateEncryptor();

                byte[] secretBytes = Encoding.UTF8.GetBytes(secret); // Encode secret consistently
                byte[] encryptedSecret = encryptor.TransformFinalBlock(secretBytes, 0, secretBytes.Length);

                // Store the IV along with the encrypted data
                byte[] iv = aes.IV;
                byte[] combinedData = new byte[iv.Length + encryptedSecret.Length];
                Array.Copy(iv, 0, combinedData, 0, iv.Length);
                Array.Copy(encryptedSecret, 0, combinedData, iv.Length, encryptedSecret.Length);

                string encryptedSecretPath = Path.Combine(Constants.LocalAppDataFolder, Constants.EncryptedSecretFileName);
                File.WriteAllBytes(encryptedSecretPath, combinedData); // Write bytes directly
            }
        }
    }

    private static byte[] GetMachineEntropy()
    {
        string deviceId = new DeviceIdBuilder().AddMachineName().AddOsVersion().AddFileToken(Path.Combine(Constants.LocalAppDataFolder, Constants.DeviceFileTokenName)).ToString();
        return deviceId.ToByteArray();
    }

    private static byte[] GetProtectedKey()
    {
        if (OperatingSystem.IsWindows())
        {
            string protectedKeyPath = Path.Combine(Constants.LocalAppDataFolder, Constants.ProtectedKeyFileName);

            if (File.Exists(protectedKeyPath))
            {
                return File.ReadAllBytes(protectedKeyPath);
            }

            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }

            byte[] entropySource = GetMachineEntropy();

            byte[] protectedKey = ProtectedData.Protect(key, entropySource, DataProtectionScope.LocalMachine);
            File.WriteAllBytes(protectedKeyPath, protectedKey);
            return protectedKey;
            // if linux or macOS then use an empty string as the password
        }
        return new byte[32];
    }
    public static void PurgePasswords()
    {
        string dbPath = Path.Combine(Constants.LocalAppDataFolder, Constants.ProtectedKeyFileName);
        if (OperatingSystem.IsMacOS())
        {
            string macosPath = dbPath.Replace("bin", "txt");
            if (File.Exists(macosPath))
            {
                File.Delete(macosPath);
            }
            MacOSKeyChainService.DeleteFromKeychain(Constants.KeychainSecretName, Constants.KeychainServiceName);
        }
        if (OperatingSystem.IsWindows())
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            string encryptedSecretPath = Path.Combine(Constants.LocalAppDataFolder, Constants.EncryptedSecretFileName);
            if (File.Exists(encryptedSecretPath))
                File.Delete(encryptedSecretPath);

            if (File.Exists(Constants.DatabasePasswordFilePath))
                File.Delete(Constants.DatabasePasswordFilePath);

            if (File.Exists(Constants.DeviceFileTokenName))
                File.Delete(Constants.DeviceFileTokenName);
        }
    }
}