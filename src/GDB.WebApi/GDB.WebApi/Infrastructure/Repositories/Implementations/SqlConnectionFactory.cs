using GDB.App.Infrastructure.Repositories.Contracts;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace GDB.App.Infrastructure.Repositories.Implementations;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("GDBConnection")
            ?? throw new InvalidOperationException("Set ConnectionStrings__GDBConnection via environment or user secrets.");
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("ConnectionStrings__GDBConnection must not be empty.");
        var options = new SqlConnectionStringBuilder(_connectionString);
        if (string.IsNullOrWhiteSpace(options.DataSource) || string.IsNullOrWhiteSpace(options.InitialCatalog))
            throw new InvalidOperationException("SQL data source and database are required.");
    }

    public DbConnection CreateConnection() => new SqlConnection(_connectionString);
}
