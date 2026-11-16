# Privacy Policy

This privacy policy applies to the Azure Key Vault Explorer or Key Vault Explorer for Azure ("the app") for desktop devices, created by Arthur Thomas (hereby referred to as "Service Provider") as an Open Source and Free GUI service. This service is intended for use "AS IS". It is designed to simplify finding and accessing secrets, certificates, and keys stored in Azure Key Vault. This Privacy Policy explains how we handle user data and ensure its security.

## Data Collection

The app does not collect telemetry or logs. The following data is stored locally on your device:

- Selected subscriptions and pinned vaults, saved in an encrypted SQLite database.
- Authentication credentials, encrypted and stored using DPAPI on Windows and Keychain on macOS.

## Data Usage

The data stored locally is used solely for the functionality of the app, such as accessing and managing secrets in your Azure Key Vault.

## Data Security

We take data security seriously. The app uses DPAPI and Keychain for encrypting the SQLite database and credentials. Authentication and credential storage are managed using the Microsoft.Identity.Client.Extensions.Msal library, following Microsoft's security guidelines.

## User Rights

You have the right to access and delete the data stored by the app. To delete the data, remove the files located in:

- macOS: `/Users/YOUR_USER_NAME/Library/Application Support/AzureKeyVaultExplorer/`
- Windows: `C:\Users\YOUR_USER_NAME\AppData\Local\AzureKeyVaultExplorer`

## Third-Party Services

The app integrates with Microsoft services for authentication and accessing Azure Key Vault. Please note that the Application utilizes third-party services that have their own Privacy Policy about handling data. Below are the links to the Privacy Policy of the third-party service providers used by the Application:
- [Microsoft Privacy Statement](https://privacy.microsoft.com/en-us/privacystatement)

## Children

The Service Provider does not use the Application to knowingly solicit data from or market to children under the age of 13.

The Service Provider does not knowingly collect personally identifiable information from children. The Service Provider encourages all children to never submit any personally identifiable information through the Application and/or Services. The Service Provider encourages parents and legal guardians to monitor their children's Internet usage and to help enforce this Policy by instructing their children never to provide personally identifiable information through the Application and/or Services without their permission. If you have reason to believe that a child has provided personally identifiable information to the Service Provider through the Application and/or Services, please contact the Service Provider so that they can take the necessary actions. You must also be at least 16 years of age to consent to the processing of your personally identifiable information in your country (in some countries we may allow your parent or guardian to do so on your behalf).

## Opt-Out Rights

You can stop all collection of information by the Application easily by uninstalling it. You may use the standard uninstall processes as may be available as part of your device or via the application marketplace or network.

## Data Retention Policy

The Service Provider will retain User Provided data for as long as you use the Application and for a reasonable time thereafter. If you'd like them to delete User Provided Data that you have provided via the Application, please contact them at [acthomasiv@gmail.com](mailto:acthomasiv@gmail.com) and they will respond in a reasonable time.

## Your Consent

By using the Application, you are consenting to the processing of your information as set forth in this Privacy Policy now and as amended by us.

## Updates to the Privacy Policy

We may update this Privacy Policy from time to time. Users will be informed of any changes through the app or our official website.

## Contact Us

If you have any questions about this Privacy Policy, please contact us at [acthomasiv@gmail.com](mailto:acthomasiv@gmail.com).
