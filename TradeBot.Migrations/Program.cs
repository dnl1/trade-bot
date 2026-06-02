using Microsoft.Extensions.Configuration;
using Npgsql;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Example.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString =
    configuration["PostgresConnectionString"] ??
    configuration["postgresConnectionString"] ??
    Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("PostgresConnectionString not configured");

var migrationsPath = Path.Combine(AppContext.BaseDirectory, "Migrations");

using var connection = new NpgsqlConnection(connectionString);
connection.Open();

EnsureMigrationTable(connection);

if (!Directory.Exists(migrationsPath))
{
    Console.WriteLine($"No migrations directory found at {migrationsPath}");
    return;
}

foreach (var migrationFile in Directory.GetFiles(migrationsPath, "*.sql").OrderBy(Path.GetFileName))
{
    var version = Path.GetFileNameWithoutExtension(migrationFile);

    if (HasMigration(connection, version))
    {
        Console.WriteLine($"Skipping {version}");
        continue;
    }

    var sql = File.ReadAllText(migrationFile);

    using var transaction = connection.BeginTransaction();

    using (var command = connection.CreateCommand())
    {
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    using (var command = connection.CreateCommand())
    {
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO schema_migrations (version)
            VALUES (@version)
            """;
        command.Parameters.AddWithValue("version", version);
        command.ExecuteNonQuery();
    }

    transaction.Commit();
    Console.WriteLine($"Applied {version}");
}

return;

static void EnsureMigrationTable(NpgsqlConnection connection)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS schema_migrations (
            version TEXT PRIMARY KEY,
            applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        )
        """;
    command.ExecuteNonQuery();
}

static bool HasMigration(NpgsqlConnection connection, string version)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT 1
        FROM schema_migrations
        WHERE version = @version
        """;
    command.Parameters.AddWithValue("version", version);

    return command.ExecuteScalar() != null;
}
