using KeyVaultExplorer.Database;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyVaultExplorer.Database;

public partial class KvExplorerDb : IDisposable
{
    private static string _password = null;

    public KvExplorerDb()
    {
    }

    public void Dispose()
    {
    }

    public async Task<bool> DeleteQuickAccessItemByKeyVaultId(string keyVaultId)
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM QuickAccess WHERE KeyVaultId = @KeyVaultId;";
        command.Parameters.Add(new SqliteParameter("@KeyVaultId", keyVaultId));

        var rowsAffected = await command.ExecuteNonQueryAsync();

        // Check if any rows were deleted (1 or more indicates success)
        return rowsAffected > 0;
    }

    public async IAsyncEnumerable<QuickAccess> GetQuickAccessItemsAsyncEnumerable(string tenantId = null)
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        var query = new StringBuilder("SELECT Id, Name, VaultUri, KeyVaultId, SubscriptionDisplayName, SubscriptionId, TenantId, Location FROM QuickAccess");

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            query.Append($" WHERE TenantId = '{tenantId}'");
        }
        query.Append(";");
        command.CommandText = query.ToString();

        var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new QuickAccess
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                VaultUri = reader.GetString(2),
                KeyVaultId = reader.GetString(3),
                SubscriptionDisplayName = reader.IsDBNull(4) ? null : reader.GetString(4),
                SubscriptionId = reader.IsDBNull(5) ? null : reader.GetString(5),
                TenantId = reader.GetString(6),
                Location = reader.GetString(7),
            };
            yield return item;
        }
    }

    public async Task<List<Subscriptions>> GetStoredSubscriptions(string tenantId = null)
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        var query = new StringBuilder("SELECT DisplayName, SubscriptionId, TenantId FROM Subscriptions");

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            query.Append($" WHERE TenantId = '{tenantId.ToUpperInvariant()}'");
        }
        query.Append(";");
        command.CommandText = query.ToString();
        var reader = command.ExecuteReader();

        var subscriptions = new List<Subscriptions>();
        while (await reader.ReadAsync())
        {
            var subscription = new Subscriptions
            {
                DisplayName = reader.GetString(0),
                SubscriptionId = reader.GetString(1),
                TenantId = reader.GetGuid(2)
            };
            subscriptions.Add(subscription);
        }
        return subscriptions;
    }

    public async Task InsertQuickAccessItemAsync(QuickAccess item)
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
                            INSERT INTO QuickAccess (Name, VaultUri, KeyVaultId, SubscriptionDisplayName, SubscriptionId, TenantId, Location)
                            VALUES (@Name, @VaultUri, @KeyVaultId, @SubscriptionDisplayName, @SubscriptionId, @TenantId, @Location);
                            """;
        command.Parameters.Add(new SqliteParameter("@Name", item.Name));
        command.Parameters.Add(new SqliteParameter("@VaultUri", item.VaultUri));
        command.Parameters.Add(new SqliteParameter("@KeyVaultId", item.KeyVaultId));
        command.Parameters.Add(new SqliteParameter("@SubscriptionDisplayName", item.SubscriptionDisplayName ?? (object)DBNull.Value));
        command.Parameters.Add(new SqliteParameter("@SubscriptionId", item.SubscriptionId ?? (object)DBNull.Value));
        command.Parameters.Add(new SqliteParameter("@TenantId", item.TenantId));
        command.Parameters.Add(new SqliteParameter("@Location", item.Location));
        await command.ExecuteNonQueryAsync();
    }

    public async Task InsertSubscriptions(IEnumerable<Subscriptions> subscriptions)
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();
        using var tx = connection.BeginTransaction();

        foreach (var subscription in subscriptions)
        {
            var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO Subscriptions (DisplayName, SubscriptionId, TenantId) VALUES (@DisplayName, @SubscriptionId, @TenantId);";
            command.Parameters.Add(new SqliteParameter("@DisplayName", subscription.DisplayName));
            command.Parameters.Add(new SqliteParameter("@SubscriptionId", subscription.SubscriptionId));
            command.Parameters.Add(new SqliteParameter("@TenantId", subscription.TenantId));
            await command.ExecuteNonQueryAsync();
        }
        tx.Commit();
    }

    public async Task<bool> QuickAccessItemByKeyVaultIdExists(string? keyVaultId)
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM QuickAccess WHERE KeyVaultId = @KeyVaultId LIMIT 1;";
        command.Parameters.Add(new SqliteParameter("@KeyVaultId", keyVaultId));

        var result = await command.ExecuteScalarAsync();
        return result is not null;
    }

    public async Task RemoveSubscriptionsBySubscriptionIDs(IEnumerable<string> subscriptionIds)
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        var paramString = new StringBuilder("DELETE FROM Subscriptions WHERE SubscriptionId IN (");

        subscriptionIds.TryGetNonEnumeratedCount(out int count);
        foreach (var (subscriptionId, index) in subscriptionIds.Select((id, index) => (id, index)))
        {
            command.Parameters.Add(new SqliteParameter("@SubscriptionId" + index, subscriptionId));
            paramString.Append(index > 0 && index > count ? ',' : "");
            paramString.Append($"@SubscriptionId{index}");
        }
        paramString.Append(')');

        command.CommandText = paramString.ToString();

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<SqliteConnection> TryCreateDatabaseAndOpenConnection()
    {
        var dbPassExists = File.Exists(Constants.DatabasePasswordFilePath);
        if (!dbPassExists)
        {
            DatabaseEncryptedPasswordManager.SetSecret($"keyvaultexplorer_{System.Guid.NewGuid().ToString()[..6]}");
        }

        _password = await DatabaseEncryptedPasswordManager.GetSecret();

        var db = new SqliteConnection($"Filename={Constants.DatabaseFilePath}; Password={_password}");

        return db;
    }

    public static async void InitializeDatabase()
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();

        string tableCommand = """
                PRAGMA foreign_keys = off;
                BEGIN TRANSACTION;
                -- Table: Subscriptions
                CREATE TABLE IF NOT EXISTS Subscriptions (
                    DisplayName    TEXT NOT NULL,
                    SubscriptionId TEXT (200) PRIMARY KEY UNIQUE ON CONFLICT IGNORE,
                    TenantId       TEXT (200)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Subscriptions_DisplayName_SubscriptionsId ON Subscriptions (
                    SubscriptionId ASC,
                    DisplayName ASC
                );
                -- Table: QuickAccess
                CREATE TABLE IF NOT EXISTS QuickAccess (
                    Id                      INTEGER NOT NULL CONSTRAINT PK_QuickAccess PRIMARY KEY AUTOINCREMENT,
                    Name                    TEXT    NOT NULL,
                    VaultUri                TEXT    NOT NULL,
                    KeyVaultId              TEXT    NOT NULL CONSTRAINT UQ_KeyVaultId UNIQUE ON CONFLICT IGNORE,
                    SubscriptionDisplayName TEXT,
                    SubscriptionId          TEXT,
                    TenantId                TEXT    NOT NULL,
                    Location                TEXT    NOT NULL
                );
                -- Index: IX_QuickAccess_KeyVaultId
                CREATE INDEX IF NOT EXISTS IX_QuickAccess_KeyVaultId ON QuickAccess (
                    KeyVaultId
                );
                COMMIT TRANSACTION;
                PRAGMA foreign_keys = on;
                """;
        var createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText = tableCommand;
        await createTableCommand.ExecuteNonQueryAsync();
    }

    public async Task DropTablesAndRecreate()
    {
        using var connection = await TryCreateDatabaseAndOpenConnection();
        await connection.OpenAsync();
        string deleteCommand = """
                PRAGMA foreign_keys = off;
                BEGIN TRANSACTION;
                DROP TABLE IF EXISTS Subscriptions;
                DROP TABLE IF EXISTS QuickAccess;
                COMMIT TRANSACTION;
                PRAGMA foreign_keys = on;
                """;
        var deleteTableCommand = connection.CreateCommand();
        deleteTableCommand.CommandText = deleteCommand;
        await deleteTableCommand.ExecuteNonQueryAsync();
        InitializeDatabase();
    }

    public async Task DeleteDatabaseFile()
    {
        if (File.Exists(Constants.DatabaseFilePath))
        {
            using var connection = await TryCreateDatabaseAndOpenConnection();
            await connection.CloseAsync();
            File.Delete(Constants.DatabaseFilePath);
            DatabaseEncryptedPasswordManager.PurgePasswords();
        }
    }
}