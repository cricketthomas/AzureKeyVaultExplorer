## Building from source / Running the application

You will need the latest version of the .NET 10 SDK, and Visual Studio 2026 (this can be downloaded from the Microsoft Store).

Clone the project, open the `.\AzureKeyVaultExplorer` directory and open the solution file called "kv.sln" or "AzureKeyVaultStudio.slnx" for version 2. 

You will also need to make sure you have the latest AvaloniaUI and Uno Platform extensions installed.

### You can also download from the [releases](https://github.com/cricketthomas/AzureKeyVaultExplorer/releases) section for exe and macOS versions.
If downloaded from this section, you will need to follow this guide to run the app: https://github.com/cricketthomas/AzureKeyVaultExplorer/discussions/67#discussioncomment-10014603

### Prerequisites

Install the latest .NET 10 SDK: https://dotnet.microsoft.com/en-us/download/dotnet

### Steps

1. Open PowerShell 7+ (on windows, Linux and mac, or zsh on mac)

2. `cd c:\repos` (choose the folder of your choice)

3. `git clone https://github.com/cricketthomas/AzureKeyVaultExplorer.git` (to clone/download the sources)

4. `cd AzureKeyVaultExplorer` (to get into the freshly cloned repo)

5. `.\build.ps1 -RunBuild -Platform net8.0 -Runtime win-x64` (other platforms include win-arm64, osx-x64, osx-arm64, linux-x64). 
<strong>To build a self contained `.exe` please run `.\build.ps1 -Runtime win-x64 -PublishAot:$false`, you can ignore the `.pdb` files. </strong>

If you get an error message stating "Platform linker not found" when building on Windows, please ensure you have all the required prerequisites documented at https://aka.ms/nativeaot-prerequisites, in particular the Desktop Development for C++ workload in Visual Studio. 

Open the Visual Studio Installer, Modify, install Desktop Development for C++
<img width="800" src="https://github.com/user-attachments/assets/867c043e-ba41-4b3e-bc68-5ef2c56f2cff"/>

For ARM64 development also install C++ ARM64 build tools. 
<img width="600" src="https://github.com/user-attachments/assets/0ddb7ef8-1378-4258-af50-d877093f121a"/>

Repeat step 5. The build starts and might take a couple of minutes. The final output looks something like this: `Desktop -> C:\Repos\AzureKeyVaultExplorer\publish\`

6. Open that folder in Windows Explorer and run `keyvaultexplorerdesktop.exe`. On macOS, a `Key Vault Explorer.app` mac os package will be generated in the publish directory. Move this to "Applications", you will have to force open the app using System Preferences, and click "Open anyway".

7. It will launch your default browser window and prompt you to login and grant consent. 

### Notes
The app is now verified as I am member of the Microsoft Partner Program. 
<p align="left">
   <img width="400" src="https://github.com/user-attachments/assets/1e7e802f-cabf-481c-8f39-b78875772ffd"/>
</p>

When you run it for the first time, it creates an enterprise application in your tenant. 
Please contact your Azure tenant admin to make sure the application has been consented for use. 
Otherwise you will get an error message, see [First time Azure Tenant setup](FIRST-TIME-SETUP.md).

<img src="https://github.com/user-attachments/assets/f1d093d6-8e4c-4c70-b917-bc62d030b6b2"/>

Alternatively, you create an enterprise application with the following permissions, then you can modify the clientId in the `Constants.cs` file to your newly created app that is hosted in your own tenant.
<img src="https://github.com/user-attachments/assets/e17754a6-728e-490b-ad74-8e87e895387a"/>
