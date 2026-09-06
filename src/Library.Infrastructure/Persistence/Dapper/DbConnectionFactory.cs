using System.Data.Common;
using Library.Application.Common.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Library.Infrastructure.Persistence.Dapper;

/// <summary>Opens a raw ADO.NET connection for the configured relational provider (used by the Dapper read paths).</summary>
public interface IDbConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Provider-specific quote for a limit/offset clause difference etc.</summary>
    string Paginate(int pageSize, int offset);
}

public sealed class DbConnectionFactory(DatabaseOptions options) : IDbConnectionFactory
{
    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        DbConnection connection = options.ResolvedProvider switch
        {
            DatabaseProvider.Postgres => new NpgsqlConnection(options.ConnectionString),
            DatabaseProvider.SqlServer => new SqlConnection(options.ConnectionString),
            DatabaseProvider.Sqlite => new SqliteConnection(options.ConnectionString),
            _ => throw new NotSupportedException(
                $"Dapper read paths support Postgres, SqlServer and Sqlite; '{options.Provider}' is not supported."),
        };

        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public string Paginate(int pageSize, int offset) => options.ResolvedProvider switch
    {
        DatabaseProvider.SqlServer => $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",
        _ => $"LIMIT {pageSize} OFFSET {offset}",
    };
}
