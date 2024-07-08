using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DeviceId;
using KeyVaultExplorer.Models;

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

                byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
                byte[] encryptedSecret = encryptor.TransformFinalBlock(secretBytes, 0, secretBytes.Length);

                byte[] iv = aes.IV;
                byte[] combinedData = new byte[iv.Length + encryptedSecret.Length];
                Array.Copy(iv, 0, combinedData, 0, iv.Length);
                Array.Copy(encryptedSecret, 0, combinedData, iv.Length, encryptedSecret.Length);

                string encryptedSecretPath = Path.Combine(Constants.LocalAppDataFolder, Constants.EncryptedSecretFileName);
                File.WriteAllBytes(encryptedSecretPath, combinedData); 
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
        }
        return new byte[32];
    }
}