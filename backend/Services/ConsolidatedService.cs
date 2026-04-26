using Oracle.ManagedDataAccess.Client;
using OBFSimple.Models;

namespace OBFSimple.Services;

public class ConsolidatedService
{
    private readonly OracleConnectionFactory _factory;

    public ConsolidatedService(OracleConnectionFactory factory) => _factory = factory;

    public ConsolidatedData GetConsolidatedData(string month)
    {
        var result = new ConsolidatedData { SubmissionMonth = month };

        using var conn = _factory.Create();
        conn.Open();

        const string collegeSql = @"
            SELECT
                COUNT(*) as CollegeCount,
                AVG(IND_2_5_R) as Avg_2_5,
                AVG(IND_3_1_R) as Avg_3_1,
                AVG(IND_3_3_R) as Avg_3_3,
                AVG(IND_3_4_R) as Avg_3_4,
                AVG(IND_4_4_R) as Avg_4_4,
                AVG(IND_4_5_R) as Avg_4_5,
                AVG(IND_5_1_R) as Avg_5_1,
                AVG(IND_6_1_R) as Avg_6_1,
                AVG(IND_6_2_R) as Avg_6_2
            FROM OBF_DATA d
            JOIN OBF_USERS u ON d.USER_ID = u.ID
            WHERE d.SUBMISSION_MONTH = :month AND u.USER_TYPE = 'college'";

        using var cmd = new OracleCommand(collegeSql, conn);
        cmd.Parameters.Add("month", month);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            result.CollegeCount = Convert.ToInt32(reader["CollegeCount"]);
            result.Ind_2_5 = Kpi(reader["Avg_2_5"], result.CollegeCount);
            result.Ind_3_1 = Kpi(reader["Avg_3_1"], result.CollegeCount);
            result.Ind_3_3 = Kpi(reader["Avg_3_3"], result.CollegeCount);
            result.Ind_3_4 = Kpi(reader["Avg_3_4"], result.CollegeCount);
            result.Ind_4_4 = Kpi(reader["Avg_4_4"], result.CollegeCount);
            result.Ind_4_5 = Kpi(reader["Avg_4_5"], result.CollegeCount);
            result.Ind_5_1 = Kpi(reader["Avg_5_1"], result.CollegeCount);
            result.Ind_6_1 = Kpi(reader["Avg_6_1"], result.CollegeCount);
            result.Ind_6_2 = Kpi(reader["Avg_6_2"], result.CollegeCount);
        }

        using var cmd2 = new OracleCommand(
            "SELECT IND_5_3_R FROM OBF_DATA d JOIN OBF_USERS u ON d.USER_ID = u.ID WHERE d.SUBMISSION_MONTH = :month AND u.USER_TYPE = 'cgs'", conn);
        cmd2.Parameters.Add("month", month);
        using var r2 = cmd2.ExecuteReader();
        if (r2.Read()) result.Ind_5_3 = Kpi(r2["IND_5_3_R"], 1);

        using var cmd3 = new OracleCommand(
            "SELECT IND_6_1_R, IND_6_2_R FROM OBF_DATA d JOIN OBF_USERS u ON d.USER_ID = u.ID WHERE d.SUBMISSION_MONTH = :month AND u.USER_TYPE = 'uod'", conn);
        cmd3.Parameters.Add("month", month);
        using var r3 = cmd3.ExecuteReader();
        if (r3.Read())
        {
            result.UodInd_6_1 = Kpi(r3["IND_6_1_R"], 1);
            result.UodInd_6_2 = Kpi(r3["IND_6_2_R"], 1);
        }

        using var cmd4 = new OracleCommand(
            "SELECT IND_4_4_R, IND_4_5_R FROM OBF_DATA d JOIN OBF_USERS u ON d.USER_ID = u.ID WHERE d.SUBMISSION_MONTH = :month AND u.USER_TYPE = 'research_office'", conn);
        cmd4.Parameters.Add("month", month);
        using var r4 = cmd4.ExecuteReader();
        if (r4.Read())
        {
            result.ResearchInd_4_4 = Kpi(r4["IND_4_4_R"], 1);
            result.ResearchInd_4_5 = Kpi(r4["IND_4_5_R"], 1);
        }

        return result;
    }

    private static ConsolidatedKpi Kpi(object value, int count)
    {
        if (value == DBNull.Value || count == 0) return new ConsolidatedKpi { Count = 0 };
        return new ConsolidatedKpi { Avg = Math.Round(Convert.ToDecimal(value), 2), Count = count };
    }
}
