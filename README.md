# Azure Key Vault Explorer

<img src="https://github.com/cricketthomas/AzureKeyVaultExplorer/assets/15821271/678b131f-58eb-452e-858c-c41ffaa89fbf" width="250" >

## Overview
Visit the releases section to download the application. *Still in active development but in a usable state*

**Key Vault Explorer** is a lightweight tool with the idea to simplify finding and accessing secrets (and certitficates and keys) stored in Azure Key Vault, providing a interface for aggregating, filtering, and quickly getting secret values. The app was inspired by the original [AzureKeyVaultExplorer](https://github.com/microsoft/AzureKeyVaultExplorer) with the goal to eventually bring some more feature parity but first brining the application to macOS.

### Key features

- Signing in with a Microsoft Account [See how credentials are secured](#security)
- Support to selectively include/exclude subscriptions to show resource groups and key vaults in the tree
- Ability to filter subscriptions, resrouce groups, and key vaults by name
- Saving vaults to "pinned" section in quick access menu and saving selected subscriptions in SQLite
- Copy secrets to the clipboard using Control+C
- Automatic clearing of clipboard values after a set amount of time (configurable up to 60 seconds)
- Viewing details and tags about values
- Filtering and sorting of values
- Viewing last updates and next to expire values
- Downloading and saving .pfx and .cer files

### Privacy Features
- **No telemetry or logs collected**
- Sqlite Database encryption using DPAPI and KeyChain on Mac
  


# Security

The authentication and credentials storage uses [Microsoft.Identity.Client.Extensions.Msal](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) library are encrypted and stored with DPAPI on windows, and the keychain on macOS (you may be prompted multiple times to grant rights).
The security is pulled directly from this document: https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache#configuring-the-token-cache

The Sqlite database is encrypted using DPAPI on windows, and on macOS the password  in the keychain.

## Screenshots


<img width="1419" alt="WinOSDark" src="https://github.com/user-attachments/assets/9b26fafb-e2b7-4f03-880c-4653fbcf0903">
<img width="1419" alt="WinOSLight" src="https://github.com/cricketthomas/AzureKeyVaultExplorer/assets/15821271/1f5e1a5b-1796-43c1-bbd9-1ee60e3deeb0">

<img width="1419" alt="MacDark" src="https://github.com/cricketthomas/KeyVaultExplorer/assets/15821271/365cea71-2a68-4cab-997c-2631922e7bc6">
<img width="1426" alt="MacLight" src="https://github.com/cricketthomas/KeyVaultExplorer/assets/15821271/41793cfa-eb01-4954-b062-56072d19d5ea">



<img width="400" alt="Light" src="https://github.com/user-attachments/assets/ed0b7919-666d-4f03-a09a-c6763fe2ca1d">
<img width="400" alt="Light" src="https://github.com/user-attachments/assets/87823029-e98d-4f4b-91d3-b7ea78f934ae">
<img width="400" alt="Light" src="https://github.com/user-attachments/assets/d33521b6-effd-4a51-b0c5-161feac56ffe">
<img width="400" alt="Light" src="https://github.com/user-attachments/assets/ec1c614d-5de1-47c4-97ce-20c0246690ea">
<!-- <img height="350" alt="Light" src="https://github.com/user-attachments/assets/8d051a29-c653-4e35-9968-388f9dcc5d07"> -->


## Running the application:

You will need the latest version of the .NET 8 SDK, and Visual Studio 2022 (this can be downloaded from the Microsoft Store).

Clone the project, open the `.\AzureKeyVaultExplorer` directory and open the solution file called "kv.sln" or "kv.slnx". 


## Building from source
### You can also download from the release section: https://github.com/cricketthomas/AzureKeyVaultExplorer/releases
If downloaded from this section, you will need to follow this guide to run the app: https://github.com/cricketthomas/AzureKeyVaultExplorer/discussions/67#discussioncomment-10014603


Install the lastest .NET 8 SDK: https://dotnet.microsoft.com/en-us/download/dotnet

1. Open PowerShell 7+ (on windows and mac, or zsh on mac)

2. `cd c:\repos` (choose the folder of your choice)

3. `git clone https://github.com/cricketthomas/AzureKeyVaultExplorer.git` (to clone/download the sources)

4. `cd AzureKeyVaultExplorer` (to get into the freshly cloned repo)

5. `.\build.ps1 -RunBuild -Platform net8.0 -Runtime win-x64` (other platforms include win-arm64, osx-x64, osx-arm64). 
<strong>To build a self contained `.exe` please run `.\build.ps1 -Runtime win-x64 -PublishAot:$false`, you can ignore the `.pdb` files. </strong>

If you get an error message stating "Platform linker not found" when building on Windows, please ensure you have all the required prerequisites documented at https://aka.ms/nativeaot-prerequisites, in particular the Desktop Development for C++ workload in Visual Studio. 

Open the Visual Studio Installer, Modify, install Desktop Development for C++
<img width="800" src="https://github.com/user-attachments/assets/867c043e-ba41-4b3e-bc68-5ef2c56f2cff"/>


For ARM64 development also install C++ ARM64 build tools. 
<img width="600" src="https://github.com/user-attachments/assets/0ddb7ef8-1378-4258-af50-d877093f121a"/>



Repeat step 5. The build starts and might take a couple of minutes. The final output looks something like this: `Desktop -> C:\Repos\AzureKeyVaultExplorer\publish\`

6. Open that folder in Windows Explorer and run `keyvaultexplorerdesktop.exe`. On macOS, a `Key Vault Explorer.app` mac os package will be generated in the publish directory. Move this to "Applications", you will have to force open the app using System Preferences, and click "Open anyway".

7. It will lauch your default browser window and prompt you to login and grant consent. 
### Notes: 
The app is now verified as I am member of the Microsoft Partner Program. 
<img width="400" src="https://github.com/user-attachments/assets/1e7e802f-cabf-481c-8f39-b78875772ffd"/>


When you run it for the first time, it creates an enterprise application in your tenant. 
Please contact your Azure tenant admin to make sure the application has been consented for use. 
Otherwise you will get an error message:
<img src="https://github.com/user-attachments/assets/f1d093d6-8e4c-4c70-b917-bc62d030b6b2"/>

Alternatively, you create an enterprise application with the following permissions, then you can modify the clientId in the `Constants.cs` file to your newly created app that is hosted in your own tenant.
<img  src="https://github.com/user-attachments/assets/e17754a6-728e-490b-ad74-8e87e895387a"/>

<sup>Thank you to reddit user AzureToujours for helping with the readme.</sub>


## Troubleshooting
The folder where all app associated data like the database and other encrypted files is `/Users/YOUR_USER_NAME/Library/Application Support/KeyVaultExplorer/` on macOS
and `C:\Users\YOUR_USER_NAME\AppData\Local\KeyVaultExplorer` on Windows.
If you're facing trouble, I recommend deleteing all files in the directory. On macOS, i also recommend opening the key chain and deleting everything that begins with "keyvaultexplorer_".

When downloading on windows, you may have to click properties on the exe/application file and check the "unblock" checkbox to allow running the application on the machine if you get a messages saying the app needs another app from the microsoft store to download.

## Contribution
Accepting PRs, suggestions, code reviews, feature requests and more. This is my first time using avaloniaUI and building a desktop application so all feedback is welcome.  


### Dependencies
- **[.NET 8](https://github.com/dotnet/runtime)**
- **[Avalonia](https://github.com/AvaloniaUI/Avalonia/)** 
- **[FluentAvalonia](https://github.com/amwx/FluentAvalonia/)**
- **Azure.ResourceManager.KeyVault**
- **Azure.Security.KeyVault.Certificates**
- **Azure.Security.KeyVault.Keys**
- **Azure.Security.KeyVault.Secrets**
- **CommunityToolkit.Mvvm**
- **Microsoft.Data.Sqlite**
- **Microsoft.Extensions.Caching.Memory**
- **Microsoft.Identity.Client.Extensions.Msal**
- **Microsoft.Extensions.DependencyInjection**
