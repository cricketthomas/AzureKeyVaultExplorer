## Troubleshooting

### App data locations

All app-associated data (database, encrypted files) is stored in:

- **macOS:** `/Users/YOUR_USER_NAME/Library/Application Support/KeyVaultExplorer/`
- **Windows:** `C:\Users\YOUR_USER_NAME\AppData\Local\KeyVaultExplorer`

### Reset options in the app

The Settings page provides built-in options for resetting app state:

- **Sign out** — Removes the current account from cache and signs you out.
- **Delete Database** — Clears the SQLite database of all saved items (pinned vaults, saved subscriptions, etc.).
- **Reset Application** — Deletes the database file and all associated protected files. This is the most thorough reset and will return the app to a fresh state.

### Manual reset

If the app won't launch or the settings page is inaccessible, manually delete all files in the app data directory listed above. On macOS, also open Keychain Access and delete everything that begins with `keyvaultexplorer_`.

### Windows "unblock" issue

When downloading on Windows, you may need to right-click the `.exe`, select **Properties**, and check the **"Unblock"** checkbox before running the application. This can happen if Windows shows a message saying the app needs another app from the Microsoft Store.