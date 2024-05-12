﻿using System;
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
    private static byte[] GetMachineEntropy()
    {
        // Generate a machine-specific entropy source using DeviceId
        //        string deviceId = new DeviceIdBuilder()
        //             .AddMachineName()
        //             .OnWindows(windows => windows

        //                 .AddMotherboardSerialNumber()
        //                 .AddSystemDriveSerialNumber())
        //#if LINUX
        //.OnLinux(linux => linux
        //.AddMotherboardSerialNumber()
        //.AddSystemDriveSerialNumber())
        //#endif
        //#if MACOS
        // .OnMac(mac => mac
        //                 .AddSystemDriveSerialNumber()
        //                 .AddPlatformSerialNumber())
        //#endif

        //             .ToString();

        string deviceId = new DeviceIdBuilder().AddMachineName().AddOsVersion().AddFileToken(Path.Combine(Constants.LocalAppDataFolder, Constants.DeviceFileTokenName)).ToString();

        return deviceId.ToByteArray();
    }

    public static void SetSecret(string secret)
    {
#if MACOS

        MacOSKeyChainService.SaveToKeychain(Constants.KeychainSecretName, Constants.KeychainServiceName, secret);
#endif
#if WINDOWS
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
#endif

    }



    public static string GetSecret()
    {


#if WINDOWS
        byte[] storedProtectedKey = GetProtectedKey();
        byte[] entropySource = GetMachineEntropy();
        byte[] unprotectedKey = ProtectedData.Unprotect(storedProtectedKey, entropySource, DataProtectionScope.LocalMachine);

        string encryptedSecretPath = Path.Combine(Constants.LocalAppDataFolder, Constants.EncryptedSecretFileName);
        if (!File.Exists(encryptedSecretPath))
            return null;

        byte[] combinedData = File.ReadAllBytes(encryptedSecretPath); // Read bytes directly

        using (var aes = Aes.Create())
        {
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
#endif
#if MACOS


            string password = MacOSKeyChainService.GetPassword(Constants.KeychainSecretName, Constants.KeychainServiceName);

            return password;
#endif
        return "";

    }

    private static byte[] GetProtectedKey()
    {
#if WINDOWS
        
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
#endif
        return new byte[32];
    }
}