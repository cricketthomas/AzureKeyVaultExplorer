## First time installs in Azure Tenant (Using default clientId)

When running Key Vault Explorer for the first time, you may encounter an error like this:

<p align="left">
<img width="450" src="https://github.com/user-attachments/assets/8bc44343-ff85-41a6-a2d3-63f3c0db2301">
</p>

This means your Azure tenant global admin needs to grant consent. Have them open one of the following URLs in a browser:

**Azure CLI CLIENT ID:**
- This approach should require no intervention if Azure CLI is used by your organization. Please see the instructions here:
[Using your own Client ID / Tenant ID](CUSTOM-CLIENT-TENANT-ID.md/#azure-cli)



**Default Client ID:**

```
https://login.microsoftonline.com/YOUR_TENANT_ID/adminconsent?client_id=fdc1e6da-d735-4627-af3e-d40377f55713
```

**Custom Client ID** (if you've [configured your own](CUSTOM-CLIENT-TENANT-ID.md)):

```
https://login.microsoftonline.com/YOUR_TENANT_ID/adminconsent?client_id=YOUR_CLIENT_ID
```

Replace `YOUR_TENANT_ID` with your Azure AD / Entra tenant ID.

## More Info:

Please follow this Microsoft Learn article if you encounter this error: https://learn.microsoft.com/en-us/answers/questions/1393470/azure-enterprise-apps-missing-a-permission-listed

> Permissions are not synchronized in real time. When you grant new permissions to a multi-tenant application in your tenant, these permissions are not synchronized to your customer tenants that use the multi-tenant application. You must run the admin consent URL in the browser and contact the global administrator of your customer tenant to sign in and consent to these permissions.


