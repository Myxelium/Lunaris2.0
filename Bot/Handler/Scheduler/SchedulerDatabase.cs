using Microsoft.Data.SqlClient;

namespace Lunaris2.Handler.Scheduler;

public static class SchedulerDatabase
{
    public static void EnsureExists(string connectionString)
    {
        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception error)
        {
            throw new InvalidOperationException("Scheduler:ConnectionString is not a valid SQL Server connection string.", error);
        }

        var database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("Scheduler:ConnectionString must include a Database value.");

        if (database.Any(character => !(char.IsLetterOrDigit(character) || character is '_' or '-')))
            throw new InvalidOperationException("Scheduler database name can only contain letters, digits, underscores, and hyphens.");

        builder.InitialCatalog = "master";
        try
        {
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                IF DB_ID(@name) IS NULL
                BEGIN
                    DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name);
                    EXEC(@sql);
                END
                """;
            command.Parameters.Add(new SqlParameter("@name", database));
            command.ExecuteNonQuery();
        }
        catch (Exception error) when (error is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Scheduler is enabled, but SQL Server could not be reached. Check Scheduler:ConnectionString and start SQL with: docker compose --profile scheduler up -d",
                error);
        }
    }
}
