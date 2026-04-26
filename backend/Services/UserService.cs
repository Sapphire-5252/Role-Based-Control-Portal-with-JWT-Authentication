using Oracle.ManagedDataAccess.Client;
using OBFSimple.Models;

namespace OBFSimple.Services;

public class UserService
{
    private readonly OracleConnectionFactory _factory;

    public UserService(OracleConnectionFactory factory) => _factory = factory;

    public List<UserInfo> GetAllUsers()
    {
        var users = new List<UserInfo>();
        using var conn = _factory.Create();
        conn.Open();

        const string sql = @"
            SELECT ID, USERNAME, FULL_NAME, ROLE, USER_TYPE, COLLEGE_NAME
            FROM OBF_USERS
            ORDER BY ROLE DESC, USER_TYPE, COLLEGE_NAME";

        using var cmd = new OracleCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            users.Add(new UserInfo
            {
                Id = Convert.ToInt32(reader["ID"]),
                Username = reader["USERNAME"].ToString()!,
                FullName = reader["FULL_NAME"]?.ToString() ?? "",
                Role = reader["ROLE"].ToString()!,
                UserType = reader["USER_TYPE"]?.ToString(),
                CollegeName = reader["COLLEGE_NAME"]?.ToString()
            });

        return users;
    }
}
