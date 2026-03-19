
<p align="center">
  <img width="280" align="center" src="src\uno\icon-iOS-Default-1024x1024@1x.png">
</p>
<h1 align="center">
  Azure Key Vault Explorer
</h1>
<p align="center">
  Find Key Vaults in Azure faster.
</p>
<p align="center">
   <a href="https://apps.microsoft.com/detail/9mz794c6t74m?cid=github_readme&mode=direct">
      <img src="https://get.microsoft.com/images/en-us%20light.svg" width="200"/>
   </a>
</p>

### Install via Winget (Windows)
```pwsh
winget install "Key Vault Explorer" --source msstore
```

### macOS and Linux
Download from the [releases page](https://github.com/cricketthomas/AzureKeyVaultExplorer/releases).

 <p style="display: block" align="center">
 	<sup>Named 'Key Vault Explorer' in the Microsoft Store.</sub>
 </p>

    
## Overview
Visit the releases section to download the application for mac and linux. 

**Key Vault Explorer** is a lightweight tool with the idea to simplify finding and accessing secrets (and certificates and keys) stored in Azure Key Vault, providing a interface for aggregating, filtering, and quickly getting secret values. The app was inspired by the original [AzureKeyVaultExplorer](https://github.com/microsoft/AzureKeyVaultExplorer) with the goal to eventually bring some more feature parity but first brining the application to macOS.

### Key features

- Signing in with a Microsoft Account [See how credentials are secured](#security)
- Support to selectively include/exclude subscriptions to show resource groups and key vaults in the tree
- Ability to filter subscriptions, resource groups, and key vaults by name
- Saving vaults to "pinned" section in quick access menu and saving selected subscriptions in SQLite
- Copy secrets to the clipboard using Control+C
- Automatic clearing of clipboard values after a set amount of time (configurable up to 60 seconds)
- Viewing details and tags about values
- Filtering and sorting of values
- Viewing last updates and next to expire values
- Downloading and saving .pfx and .cer files
- Support for custom ClientId and TenantId
- AzureGovernment, AzureChina and AzureGermany Support

### Privacy Features
- **No telemetry or logs collected**
- SQLite Database encryption using DPAPI and KeyChain on Mac
  

# Security

The authentication and credentials storage uses [Microsoft.Identity.Client.Extensions.Msal](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) library are encrypted and stored with DPAPI on windows, and the keychain on macOS (you may be prompted multiple times to grant rights).
The security is pulled directly from this document: https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache#configuring-the-token-cache

The SQLite database is encrypted using DPAPI on windows, and on macOS the password  in the keychain.

## Screenshots

<img width="1322" height="868" alt="image" src="https://github.com/user-attachments/assets/ad5d6b46-17ff-4e5b-b8b3-fc949681192f" />


<img width="1527" height="1088" alt="Screenshot 2026-03-18 at 7 21 15 PM" src="https://github.com/user-attachments/assets/5a0024e3-7122-434a-938b-cc5a1cfa5542" />


## Download

### Get it from the [releases](https://github.com/cricketthomas/AzureKeyVaultExplorer/releases) page or the Microsoft Store.

#### macOS
After downloading the `.app` bundle, macOS may block it because it's from an unidentified developer. To fix this, run:

```bash
xattr -cr "/path/to/Key Vault Explorer.app"
```

Then move the app to your **Applications** folder. You may also need to go to **System Settings > Privacy & Security** and click **"Open Anyway"**.

#### Windows
After downloading the `.exe`, Windows may block it (unless you got it from winget or Microsoft Store). Right-click the file, select **Properties**, and check the **"Unblock"** checkbox at the bottom, then click **OK**.

> [!WARNING]
> After downloading for the first time you will need to follow the first time setup guide listed below:


## Documentation
- [Building from source](docs/BUILDING.md)
- [Using your own Client ID / Tenant ID](docs/CUSTOM-CLIENT-TENANT-ID.md)
- [First time Azure Tenant setup](docs/FIRST-TIME-SETUP.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)


## Acknowledgements

<img src="https://www.masterpackager.com/media/site/img/icon_blue.svg" width="100"/>

Installer built with: [Master Packager Dev](https://www.masterpackager.com/masterpackagerdev/)

### Dependencies
- **[.NET 10](https://github.com/dotnet/runtime)**
- **[Avalonia](https://github.com/AvaloniaUI/Avalonia/)** 
- **[Uno Platform](https://github.com/unoplatform/uno)** 
- **[FluentAvalonia](https://github.com/amwx/FluentAvalonia/)**
