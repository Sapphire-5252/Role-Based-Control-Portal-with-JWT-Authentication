using Oracle.ManagedDataAccess.Client;
using OBFSimple.Models;
using System.Globalization;

namespace OBFSimple.Services;

public class ObfDataService
{
    private readonly OracleConnectionFactory _factory;

    public ObfDataService(OracleConnectionFactory factory) => _factory = factory;

    // ==================== VALIDATION ====================

    public string? ValidateData(OBFData data)
    {
        if (string.IsNullOrWhiteSpace(data.SubmissionMonth) || data.SubmissionMonth.Length != 7)
            return "Submission month must be in yyyy-MM format.";

        if (!DateTime.TryParseExact(data.SubmissionMonth + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return "Invalid submission month format.";

        if (parsedDate > new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1))
            return "Submission month cannot be in the future.";

        var mechanisms = new (string, string?)[]
        {
            ("2.5", data.Ind_2_5_Mech), ("3.1", data.Ind_3_1_Mech), ("3.3", data.Ind_3_3_Mech),
            ("3.4", data.Ind_3_4_Mech), ("4.4", data.Ind_4_4_Mech), ("4.5", data.Ind_4_5_Mech),
            ("5.1", data.Ind_5_1_Mech), ("5.3", data.Ind_5_3_Mech), ("6.1", data.Ind_6_1_Mech), ("6.2", data.Ind_6_2_Mech)
        };
        foreach (var (name, value) in mechanisms)
            if (value != null && value.Length > 255) return $"KPI {name}: mechanism text must not exceed 255 characters.";

        var percentages = new (string, decimal?, decimal?)[]
        {
            ("2.5", data.Ind_2_5_D, data.Ind_2_5_N), ("3.1", data.Ind_3_1_D, data.Ind_3_1_N),
            ("3.3", data.Ind_3_3_D, data.Ind_3_3_N), ("4.4", data.Ind_4_4_D, data.Ind_4_4_N),
            ("5.3", data.Ind_5_3_D, data.Ind_5_3_N)
        };
        foreach (var (name, d, n) in percentages)
        {
            if (d < 0) return $"KPI {name}: denominator cannot be negative.";
            if (n < 0) return $"KPI {name}: numerator cannot be negative.";
            if (d > 0 && n > d) return $"KPI {name}: numerator cannot exceed denominator.";
        }

        var numerics = new (string, decimal?)[]
        {
            ("3.4", data.Ind_3_4_D), ("4.5", data.Ind_4_5_D), ("5.1", data.Ind_5_1_D),
            ("6.1", data.Ind_6_1_D), ("6.2", data.Ind_6_2_D)
        };
        foreach (var (name, value) in numerics)
            if (value < 0) return $"KPI {name}: value cannot be negative.";

        return null;
    }

    // ==================== READ ====================

    public PagedResult<OBFData> GetAllData(int page = 1, int pageSize = 50)
    {
        using var conn = _factory.Create();
        conn.Open();
        var offset = (page - 1) * pageSize;
        const string countSql = "SELECT COUNT(*) FROM OBF_DATA";
        using var countCmd = new OracleCommand(countSql, conn);
        var total = Convert.ToInt32(countCmd.ExecuteScalar());
        const string sql = @"
            SELECT * FROM (
                SELECT d.*, u.FULL_NAME as USER_FULL_NAME, u.USER_TYPE, u.COLLEGE_NAME,
                       ROW_NUMBER() OVER (ORDER BY d.SUBMISSION_MONTH DESC, d.COLLEGE_KEY) rn
                FROM OBF_DATA d JOIN OBF_USERS u ON d.USER_ID = u.ID
            ) WHERE rn > :offset AND rn <= :limit";
        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add("offset", offset);
        cmd.Parameters.Add("limit", offset + pageSize);
        using var reader = cmd.ExecuteReader();
        var items = new List<OBFData>();
        while (reader.Read()) items.Add(Map(reader));
        return new PagedResult<OBFData> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public PagedResult<OBFData> GetDataByCollegeKey(string collegeKey, int page = 1, int pageSize = 50)
    {
        using var conn = _factory.Create();
        conn.Open();
        var offset = (page - 1) * pageSize;
        const string countSql = "SELECT COUNT(*) FROM OBF_DATA WHERE COLLEGE_KEY = :collegeKey";
        using var countCmd = new OracleCommand(countSql, conn);
        countCmd.Parameters.Add("collegeKey", collegeKey);
        var total = Convert.ToInt32(countCmd.ExecuteScalar());
        const string sql = @"
            SELECT * FROM (
                SELECT d.*, u.FULL_NAME as USER_FULL_NAME, u.USER_TYPE, u.COLLEGE_NAME,
                       ROW_NUMBER() OVER (ORDER BY d.SUBMISSION_MONTH DESC) rn
                FROM OBF_DATA d JOIN OBF_USERS u ON d.USER_ID = u.ID
                WHERE d.COLLEGE_KEY = :collegeKey
            ) WHERE rn > :offset AND rn <= :limit";
        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add("collegeKey", collegeKey);
        cmd.Parameters.Add("offset", offset);
        cmd.Parameters.Add("limit", offset + pageSize);
        using var reader = cmd.ExecuteReader();
        var items = new List<OBFData>();
        while (reader.Read()) items.Add(Map(reader));
        return new PagedResult<OBFData> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public OBFData? GetDataById(int id)
    {
        using var conn = _factory.Create();
        conn.Open();
        const string sql = @"
            SELECT d.*, u.FULL_NAME as USER_FULL_NAME, u.USER_TYPE, u.COLLEGE_NAME
            FROM OBF_DATA d JOIN OBF_USERS u ON d.USER_ID = u.ID
            WHERE d.ID = :id";
        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add("id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public bool CollegeMonthExists(string collegeKey, string month, int? excludeId = null)
    {
        using var conn = _factory.Create();
        conn.Open();
        var sql = excludeId.HasValue
            ? "SELECT COUNT(*) FROM OBF_DATA WHERE COLLEGE_KEY = :collegeKey AND SUBMISSION_MONTH = :month AND ID != :excludeId"
            : "SELECT COUNT(*) FROM OBF_DATA WHERE COLLEGE_KEY = :collegeKey AND SUBMISSION_MONTH = :month";
        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add("collegeKey", collegeKey);
        cmd.Parameters.Add("month", month);
        if (excludeId.HasValue) cmd.Parameters.Add("excludeId", excludeId.Value);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // ==================== WRITE ====================

    public int CreateData(OBFData data, int userId, string username, string collegeKey)
    {
        ComputePercentages(data);
        using var conn = _factory.Create();
        conn.Open();
        const string sql = @"
            INSERT INTO OBF_DATA (
                USER_ID, COLLEGE_KEY, SUBMISSION_MONTH,
                IND_2_5_D, IND_2_5_N, IND_2_5_R, IND_2_5_MECH,
                IND_3_1_D, IND_3_1_N, IND_3_1_R, IND_3_1_MECH,
                IND_3_3_D, IND_3_3_N, IND_3_3_R, IND_3_3_MECH,
                IND_3_4_D, IND_3_4_R, IND_3_4_MECH,
                IND_4_4_D, IND_4_4_N, IND_4_4_R, IND_4_4_MECH,
                IND_4_5_D, IND_4_5_R, IND_4_5_MECH,
                IND_5_1_D, IND_5_1_R, IND_5_1_MECH,
                IND_5_3_D, IND_5_3_N, IND_5_3_R, IND_5_3_MECH,
                IND_6_1_D, IND_6_1_R, IND_6_1_MECH,
                IND_6_2_D, IND_6_2_R, IND_6_2_MECH,
                CREATED_BY
            ) VALUES (
                :userId, :collegeKey, :submissionMonth,
                :d25, :n25, :r25, :m25,
                :d31, :n31, :r31, :m31,
                :d33, :n33, :r33, :m33,
                :d34, :r34, :m34,
                :d44, :n44, :r44, :m44,
                :d45, :r45, :m45,
                :d51, :r51, :m51,
                :d53, :n53, :r53, :m53,
                :d61, :r61, :m61,
                :d62, :r62, :m62,
                :createdBy
            )";
        using var cmd = new OracleCommand(sql, conn);
        AddParams(cmd, data);
        cmd.Parameters.Add("userId", userId);
        cmd.Parameters.Add("collegeKey", collegeKey);
        cmd.Parameters.Add("submissionMonth", data.SubmissionMonth);
        cmd.Parameters.Add("createdBy", username);
        cmd.ExecuteNonQuery();
        using var idCmd = new OracleCommand("SELECT OBF_DATA_SEQ.CURRVAL FROM DUAL", conn);
        return Convert.ToInt32(idCmd.ExecuteScalar());
    }

    public bool UpdateData(int id, OBFData data, string username, string collegeKey, bool isAdmin)
    {
        var existing = GetDataById(id);
        if (existing == null) return false;
        if (!isAdmin && existing.CollegeKey != collegeKey) return false;
        ComputePercentages(data);
        using var conn = _factory.Create();
        conn.Open();
        const string sql = @"
            UPDATE OBF_DATA SET
                IND_2_5_D = :d25, IND_2_5_N = :n25, IND_2_5_R = :r25, IND_2_5_MECH = :m25,
                IND_3_1_D = :d31, IND_3_1_N = :n31, IND_3_1_R = :r31, IND_3_1_MECH = :m31,
                IND_3_3_D = :d33, IND_3_3_N = :n33, IND_3_3_R = :r33, IND_3_3_MECH = :m33,
                IND_3_4_D = :d34, IND_3_4_R = :r34, IND_3_4_MECH = :m34,
                IND_4_4_D = :d44, IND_4_4_N = :n44, IND_4_4_R = :r44, IND_4_4_MECH = :m44,
                IND_4_5_D = :d45, IND_4_5_R = :r45, IND_4_5_MECH = :m45,
                IND_5_1_D = :d51, IND_5_1_R = :r51, IND_5_1_MECH = :m51,
                IND_5_3_D = :d53, IND_5_3_N = :n53, IND_5_3_R = :r53, IND_5_3_MECH = :m53,
                IND_6_1_D = :d61, IND_6_1_R = :r61, IND_6_1_MECH = :m61,
                IND_6_2_D = :d62, IND_6_2_R = :r62, IND_6_2_MECH = :m62,
                UPDATED_BY = :updatedBy,
                UPDATED_AT = SYSTIMESTAMP
            WHERE ID = :id";
        using var cmd = new OracleCommand(sql, conn);
        AddParams(cmd, data);
        cmd.Parameters.Add("updatedBy", username);
        cmd.Parameters.Add("id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteData(int id, string collegeKey, bool isAdmin)
    {
        if (!isAdmin)
        {
            var existing = GetDataById(id);
            if (existing == null || existing.CollegeKey != collegeKey) return false;
        }
        using var conn = _factory.Create();
        conn.Open();
        using var cmd = new OracleCommand("DELETE FROM OBF_DATA WHERE ID = :id", conn);
        cmd.Parameters.Add("id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // ==================== HELPERS ====================

    private static void ComputePercentages(OBFData data)
    {
        data.Ind_2_5_R = Pct(data.Ind_2_5_N, data.Ind_2_5_D);
        data.Ind_3_1_R = Pct(data.Ind_3_1_N, data.Ind_3_1_D);
        data.Ind_3_3_R = Pct(data.Ind_3_3_N, data.Ind_3_3_D);
        data.Ind_4_4_R = Pct(data.Ind_4_4_N, data.Ind_4_4_D);
        data.Ind_5_3_R = Pct(data.Ind_5_3_N, data.Ind_5_3_D);
        data.Ind_3_4_R = data.Ind_3_4_D;
        data.Ind_4_5_R = data.Ind_4_5_D;
        data.Ind_5_1_R = data.Ind_5_1_D;
        data.Ind_6_1_R = data.Ind_6_1_D;
        data.Ind_6_2_R = data.Ind_6_2_D;
    }

    private static decimal? Pct(decimal? n, decimal? d)
        => n == null || d == null || d == 0 ? null : Math.Round(n.Value / d.Value * 100, 2);

    private static OBFData Map(OracleDataReader r) => new()
    {
        Id = Convert.ToInt32(r["ID"]),
        UserId = Convert.ToInt32(r["USER_ID"]),
        CollegeKey = r["COLLEGE_KEY"].ToString()!,
        SubmissionMonth = r["SUBMISSION_MONTH"].ToString()!,
        Ind_2_5_D = Dec(r, "IND_2_5_D"), Ind_2_5_N = Dec(r, "IND_2_5_N"), Ind_2_5_R = Dec(r, "IND_2_5_R"), Ind_2_5_Mech = Str(r, "IND_2_5_MECH"),
        Ind_3_1_D = Dec(r, "IND_3_1_D"), Ind_3_1_N = Dec(r, "IND_3_1_N"), Ind_3_1_R = Dec(r, "IND_3_1_R"), Ind_3_1_Mech = Str(r, "IND_3_1_MECH"),
        Ind_3_3_D = Dec(r, "IND_3_3_D"), Ind_3_3_N = Dec(r, "IND_3_3_N"), Ind_3_3_R = Dec(r, "IND_3_3_R"), Ind_3_3_Mech = Str(r, "IND_3_3_MECH"),
        Ind_3_4_D = Dec(r, "IND_3_4_D"), Ind_3_4_R = Dec(r, "IND_3_4_R"), Ind_3_4_Mech = Str(r, "IND_3_4_MECH"),
        Ind_4_4_D = Dec(r, "IND_4_4_D"), Ind_4_4_N = Dec(r, "IND_4_4_N"), Ind_4_4_R = Dec(r, "IND_4_4_R"), Ind_4_4_Mech = Str(r, "IND_4_4_MECH"),
        Ind_4_5_D = Dec(r, "IND_4_5_D"), Ind_4_5_R = Dec(r, "IND_4_5_R"), Ind_4_5_Mech = Str(r, "IND_4_5_MECH"),
        Ind_5_1_D = Dec(r, "IND_5_1_D"), Ind_5_1_R = Dec(r, "IND_5_1_R"), Ind_5_1_Mech = Str(r, "IND_5_1_MECH"),
        Ind_5_3_D = Dec(r, "IND_5_3_D"), Ind_5_3_N = Dec(r, "IND_5_3_N"), Ind_5_3_R = Dec(r, "IND_5_3_R"), Ind_5_3_Mech = Str(r, "IND_5_3_MECH"),
        Ind_6_1_D = Dec(r, "IND_6_1_D"), Ind_6_1_R = Dec(r, "IND_6_1_R"), Ind_6_1_Mech = Str(r, "IND_6_1_MECH"),
        Ind_6_2_D = Dec(r, "IND_6_2_D"), Ind_6_2_R = Dec(r, "IND_6_2_R"), Ind_6_2_Mech = Str(r, "IND_6_2_MECH"),
        CreatedBy = Str(r, "CREATED_BY"), CreatedAt = r["CREATED_AT"] as DateTime?,
        UpdatedBy = Str(r, "UPDATED_BY"), UpdatedAt = r["UPDATED_AT"] as DateTime?,
        UserFullName = Str(r, "USER_FULL_NAME"), UserType = Str(r, "USER_TYPE"), CollegeName = Str(r, "COLLEGE_NAME")
    };

    private static decimal? Dec(OracleDataReader r, string col)
        => r[col] == DBNull.Value ? null : Convert.ToDecimal(r[col]);

    private static string? Str(OracleDataReader r, string col)
        => r[col] == DBNull.Value ? null : r[col].ToString();

    private static void AddParams(OracleCommand cmd, OBFData d)
    {
        void Add(string n, object? v) => cmd.Parameters.Add(n, v ?? DBNull.Value);
        Add("d25", d.Ind_2_5_D); Add("n25", d.Ind_2_5_N); Add("r25", d.Ind_2_5_R); Add("m25", d.Ind_2_5_Mech);
        Add("d31", d.Ind_3_1_D); Add("n31", d.Ind_3_1_N); Add("r31", d.Ind_3_1_R); Add("m31", d.Ind_3_1_Mech);
        Add("d33", d.Ind_3_3_D); Add("n33", d.Ind_3_3_N); Add("r33", d.Ind_3_3_R); Add("m33", d.Ind_3_3_Mech);
        Add("d34", d.Ind_3_4_D); Add("r34", d.Ind_3_4_R); Add("m34", d.Ind_3_4_Mech);
        Add("d44", d.Ind_4_4_D); Add("n44", d.Ind_4_4_N); Add("r44", d.Ind_4_4_R); Add("m44", d.Ind_4_4_Mech);
        Add("d45", d.Ind_4_5_D); Add("r45", d.Ind_4_5_R); Add("m45", d.Ind_4_5_Mech);
        Add("d51", d.Ind_5_1_D); Add("r51", d.Ind_5_1_R); Add("m51", d.Ind_5_1_Mech);
        Add("d53", d.Ind_5_3_D); Add("n53", d.Ind_5_3_N); Add("r53", d.Ind_5_3_R); Add("m53", d.Ind_5_3_Mech);
        Add("d61", d.Ind_6_1_D); Add("r61", d.Ind_6_1_R); Add("m61", d.Ind_6_1_Mech);
        Add("d62", d.Ind_6_2_D); Add("r62", d.Ind_6_2_R); Add("m62", d.Ind_6_2_Mech);
    }
}
