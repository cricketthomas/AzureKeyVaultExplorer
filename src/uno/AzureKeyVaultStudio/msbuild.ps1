
param(
    [string]$BuildNumber = '2.1.1.0',
    [string]$OutputPath = 'C:\temp\output'
)

msbuild .\AzureKeyVaultStudio.csproj /t:Restore,Build /r /p:TargetFramework='net10.0-windows10.0.26100' /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=StoreUpload  /p:AppxManifestIdentityVersion=$BuildNumber /p:AppxPackageDir="$OutputPath\$BuildNumber";

msbuild .\AzureKeyVaultStudio.csproj /t:Restore,Build /r /p:TargetFramework='net10.0-windows10.0.26100' /p:Configuration=Release /p:Platform=x86 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=StoreUpload  /p:AppxManifestIdentityVersion=$BuildNumber /p:AppxPackageDir="$OutputPath\$BuildNumber";    

msbuild .\AzureKeyVaultStudio.csproj /t:Restore,Build /r /p:TargetFramework='net10.0-windows10.0.26100' /p:Configuration=Release /p:Platform=arm64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=StoreUpload  /p:AppxManifestIdentityVersion=$BuildNumber /p:AppxPackageDir="$OutputPath\$BuildNumber";
