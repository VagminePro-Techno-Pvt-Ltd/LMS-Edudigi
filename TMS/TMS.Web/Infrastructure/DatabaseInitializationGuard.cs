using Microsoft.Data.SqlClient;

namespace TMS.Web.Infrastructure;

public static class DatabaseInitializationGuard
{
    public static bool ShouldRun(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!configuration.GetValue("DatabaseInitialization:Enabled", false))
            return false;

        var disposableConfirmed = configuration.GetValue(
            "DatabaseInitialization:DisposableDatabaseConfirmed",
            false);
        var expectedDatabase = configuration["DatabaseInitialization:ExpectedDatabaseName"]?.Trim();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (environment.IsProduction())
            throw new InvalidOperationException("Database initialization is prohibited in Production.");
        if (!disposableConfirmed)
            throw new InvalidOperationException("Database initialization requires explicit disposable-database confirmation.");
        if (string.IsNullOrWhiteSpace(expectedDatabase) || string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Database initialization requires an expected database name and configured connection string.");

        var actualDatabase = new SqlConnectionStringBuilder(connectionString).InitialCatalog?.Trim();
        if (string.IsNullOrWhiteSpace(actualDatabase) ||
            !string.Equals(actualDatabase, expectedDatabase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Configured database does not match the explicitly confirmed disposable database.");
        }

        return true;
    }
}
