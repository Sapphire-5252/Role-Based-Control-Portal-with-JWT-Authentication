using Oracle.ManagedDataAccess.Client;
using OBFSimple.Models;

namespace OBFSimple.Services;

public class AuthService
{
    private readonly OracleConnectionFactory _factory;

    public AuthService(OracleConnectionFactory factory) => _factory = factory;

    public User? ValidateUser(string username, string password)
    {
        using var conn = _factory.Create();
        conn.Open();

        const string sql = @"
            SELECT ID, USERNAME, PASSWORD_HASH, FULL_NAME, ROLE, USER_TYPE, COLLEGE_NAME
            FROM OBF_USERS
            WHERE UPPER(USERNAME) = UPPER(:username)";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add("username", username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, reader["PASSWORD_HASH"].ToString()!))
            return null;

        return new User
        {
            Id = Convert.ToInt32(reader["ID"]),
            Username = reader["USERNAME"].ToString()!,
            FullName = reader["FULL_NAME"]?.ToString() ?? "",
            Role = reader["ROLE"].ToString()!,
            UserType = reader["USER_TYPE"]?.ToString(),
            CollegeName = reader["COLLEGE_NAME"]?.ToString()
        };
    }
}
