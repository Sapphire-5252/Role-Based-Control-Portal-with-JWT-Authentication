using Oracle.ManagedDataAccess.Client;

namespace OBFSimple.Services;

public class OracleConnectionFactory
{
    private readonly string _connectionString;

    public OracleConnectionFactory(IConfiguration config)
        => _connectionString = config.GetConnectionString("Oracle")
            ?? throw new InvalidOperationException("Oracle connection string is not configured.");

    public OracleConnection Create() => new(_connectionString);
}
