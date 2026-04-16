## Using your own Client ID / Application ID

### Easiest Option:

### Azure CLI

> [!NOTE]
> You can use a custom client ID that belongs to Microsoft Azure CLI `04b07795-8ddb-461a-bbee-02f9e1bf7b46`. This is intended for testing only and is not recommended by the maintainers of this repository, as it may violate Microsoft’s Terms of Service.
> Doing so can bypass the need for an IT Administrator to grant permissions to the application enabling a zero change setup in most tenants and is enterprise friendly. THis effectively circumventing the standard consent process. Navigate to the Ssttings page, check the "Custom Client ID" checkbox, add the aformentioned Clinet Id, click save, and restart the application.
> See this article for more details on the well-known client ID.  https://rakhesh.com/azure/well-known-client-ids/

<img width="800"  alt="image" src="https://github.com/user-attachments/assets/fe6a970c-b0bc-455c-9775-5aa57b865fd1" />


---

This allows you to use your own enterprise application instead of the default one. Requires the checkbox to be selected and a valid Client ID.

1. Create an Enterprise application in your Azure AD / Entra tenant:

   ![image](https://github.com/user-attachments/assets/c72875a5-ef34-4157-8b2a-bed9f14b4b55)

   ![image](https://github.com/user-attachments/assets/e0e90e41-c649-4b4a-80a7-74c897ace4bb)

2. Select a tenant auth type:

   ![image](https://github.com/user-attachments/assets/f92a9f0b-a6cc-4e47-8f95-a9043e07bf50)

3. Navigate to **App Registrations** and go to the **Manage > Authentication** page:

   ![image](https://github.com/user-attachments/assets/dadf5b96-6364-41f8-8e2f-e4ea9855c39a)

4. Select **Desktop + Devices** and check the following boxes. Add `http://localhost` as a custom redirect URI:

   ![image](https://github.com/user-attachments/assets/3b150988-a189-4429-b29b-b4d4723d6a9e)

5. Add macOS redirect URIs:

   ![image](https://github.com/user-attachments/assets/43819626-1e95-4de7-8f75-5180761c6eb1)

6. Navigate to the **API Permissions** section and add the following permissions. You may need an admin to grant consent:

   ![image](https://github.com/user-attachments/assets/c20b5e9a-ac23-4710-bc15-a4c5dd9e843e)

7. You or an admin will have to grant consent to your own application if not granted already:

   ![image](https://github.com/user-attachments/assets/8845b9fd-5fed-42a2-bd47-0ab06575c1f0)

8. Open the app and update the Client ID in the settings page, then restart the app:

   ![image](https://github.com/user-attachments/assets/87d75793-5c95-488e-b4d4-20af6b5f46bb)

9. Upon restart you'll see something similar to this:

   ![image](https://github.com/user-attachments/assets/3913632e-0f39-435e-a285-e3ec42975132)

The app should now work as normal under your own identity and your own tenant's enterprise application.

> **Tip:** You can also set your own Client ID in `Constants.cs` and rebuild the application from source. See [Building from source](BUILDING.md).



## Using your own Tenant ID / Directory ID

By default, the app signs in using the common Microsoft endpoint and takes the first tenant available. If your organization requires you to authenticate against a specific Azure AD / Entra tenant, you can configure a custom Tenant ID in the settings:

1. Open the app and navigate to the **Settings** page.
2. Check **"Use custom Tenant ID / Directory ID (requires application restart)"**.
3. Enter your Azure Tenant ID (e.g. `18405e16-1ba4aca2-...`).
4. Restart the application.

This is useful when your tenant admin has restricted sign-in to a specific directory, or when you want to ensure you are authenticating against the correct tenant.


![image](https://github.com/user-attachments/assets/363a767f-7e86-4378-94fd-ed722b38f260)

