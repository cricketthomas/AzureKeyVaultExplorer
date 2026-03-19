using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KeyVaultExplorer.Services;

public static class MacOSKeyChainService
{
    private const string SecurityLibrary = "/System/Library/Frameworks/Security.framework/Security";

    /// <summary>
    /// Delete a generic password from the login keychain.
    /// Falls back to the `security` CLI if the native API fails.
    /// </summary>
    public static void DeleteFromKeychain(string serviceName, string accountName)
    {
        try
        {
            var kc = GetDefaultKeychain();
            var rc = SecKeychainFindGenericPassword(
                kc,
                (uint)serviceName.Length, serviceName,
                (uint)accountName.Length, accountName,
                out _,
                out _,
                out var itemRef);

            if (rc != 0)
                throw new Exception($"SecKeychainFindGenericPassword returned {rc}");

            if (itemRef == IntPtr.Zero)
                throw new Exception("No matching item found in the keychain.");

            rc = SecKeychainItemDelete(itemRef);
            if (rc != 0)
                throw new Exception($"SecKeychainItemDelete returned {rc}");
        }
        catch
        {
            // Native failed — fall back to security CLI
            DeleteFromKeychainCli(serviceName, accountName);
        }
    }

    /// <summary>
    /// Retrieve a generic password from the login keychain.
    /// Falls back to the `security` CLI if native API fails.
    /// </summary>
    public static string GetPassword(string serviceName, string accountName)
    {
        try
        {
            var kc = GetDefaultKeychain();
            var rc = SecKeychainFindGenericPassword(
                kc,
                (uint)serviceName.Length, serviceName,
                (uint)accountName.Length, accountName,
                out var pwdLen,
                out var pwdData,
                out _);

            if (rc != 0)
                throw new Exception($"SecKeychainFindGenericPassword returned {rc}");

            if (pwdData == IntPtr.Zero || pwdLen == 0)
                throw new Exception("No password data returned.");

            try
            {
                var buf = new byte[pwdLen];
                Marshal.Copy(pwdData, buf, 0, (int)pwdLen);
                return Encoding.UTF8.GetString(buf);
            }
            finally
            {
                SecKeychainItemFreeContent(IntPtr.Zero, pwdData);
            }
        }
        catch
        {
            // Native failed — fall back to security CLI
            return GetPasswordCli(serviceName, accountName);
        }
    }

    public static Task<string> GetPasswordAsync(string serviceName, string accountName) =>
            Task.Run(() => GetPassword(serviceName, accountName));

    /// <summary>
    /// Save (or update) a generic password into the login keychain.
    /// Falls back to the `security` CLI if the native API returns an error.
    /// </summary>
    public static void SaveToKeychain(string serviceName, string accountName, string password)
    {
        try
        {
            var kc = GetDefaultKeychain();
            var pwdPtr = Marshal.StringToCoTaskMemAnsi(password);
            try
            {
                var rc = SecKeychainAddGenericPassword(
                    kc,
                    (uint)serviceName.Length, serviceName,
                    (uint)accountName.Length, accountName,
                    (uint)password.Length, pwdPtr,
                    out _);

                if (rc != 0)
                    throw new Exception($"SecKeychainAddGenericPassword returned {rc}");
            }
            finally
            {
                Marshal.FreeCoTaskMem(pwdPtr);
            }
        }
        catch
        {
            // Native failed — fall back to security CLI
            SaveToKeychainCli(serviceName, accountName, password);
        }
    }

    /// <summary>
    /// Async wrappers
    /// </summary>
    public static Task SetPasswordAsync(string serviceName, string accountName, string password) =>
        Task.Run(() => SaveToKeychain(serviceName, accountName, password));

    private static void DeleteFromKeychainCli(string service, string account)
    {
        var psi = new ProcessStartInfo("security",
            $"delete-generic-password -a {EscapeArg(account)} -s {EscapeArg(service)}")
        {
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi);
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new Exception($"security CLI delete-generic-password failed: {proc.StandardError.ReadToEnd()}");
    }

    private static string EscapeArg(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";

    private static IntPtr GetDefaultKeychain()
    {
        var rc = SecKeychainCopyDefault(out var kc);
        if (rc != 0)
            throw new Exception($"SecKeychainCopyDefault failed: {rc}");
        // If the keychain is locked, this will trigger the system prompt
        SecKeychainUnlock(kc, 0, IntPtr.Zero, true);
        return kc;
    }

    private static string GetPasswordCli(string service, string account)
    {
        var psi = new ProcessStartInfo("security",
            $"find-generic-password -a {EscapeArg(account)} -s {EscapeArg(service)} -w")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi);
        var output = proc.StandardOutput.ReadToEnd().TrimEnd('\n');
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new Exception("security CLI find-generic-password failed to locate the secret.");
        return output;
    }

    private static void SaveToKeychainCli(string service, string account, string password)
    {
        var psi = new ProcessStartInfo("security",
            $"add-generic-password -a {EscapeArg(account)} -s {EscapeArg(service)} -w {EscapeArg(password)} -U")
        {
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi);
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new Exception($"security CLI add-generic-password failed: {proc.StandardError.ReadToEnd()}");
    }

    [DllImport(SecurityLibrary)]
    private static extern int SecKeychainAddGenericPassword(
            IntPtr keychain,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            uint passwordLength,
            IntPtr passwordData,
            out IntPtr itemRef);

    // P/Invokes into Security.framework
    [DllImport(SecurityLibrary)]
    private static extern int SecKeychainCopyDefault(out IntPtr defaultKeychain);

    [DllImport(SecurityLibrary)]
    private static extern int SecKeychainFindGenericPassword(
        IntPtr keychain,
        uint serviceNameLength,
        string serviceName,
        uint accountNameLength,
        string accountName,
        out uint passwordLength,
        out IntPtr passwordData,
        out IntPtr itemRef);

    [DllImport(SecurityLibrary)]
    private static extern int SecKeychainItemDelete(IntPtr itemRef);

    [DllImport(SecurityLibrary)]
    private static extern int SecKeychainItemFreeContent(
        IntPtr attrList,
        IntPtr data);

    [DllImport(SecurityLibrary)]
    private static extern int SecKeychainUnlock(
        IntPtr keychain,
        uint seconds,
        IntPtr initialData,
        bool useUI);
}