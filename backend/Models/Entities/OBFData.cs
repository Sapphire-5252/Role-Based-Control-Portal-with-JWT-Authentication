namespace OBFSimple.Models;

public class OBFData
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CollegeKey { get; set; } = "";
    public string SubmissionMonth { get; set; } = "";

    public decimal? Ind_2_5_D { get; set; }
    public decimal? Ind_2_5_N { get; set; }
    public decimal? Ind_2_5_R { get; set; }
    public string? Ind_2_5_Mech { get; set; }

    public decimal? Ind_3_1_D { get; set; }
    public decimal? Ind_3_1_N { get; set; }
    public decimal? Ind_3_1_R { get; set; }
    public string? Ind_3_1_Mech { get; set; }

    public decimal? Ind_3_3_D { get; set; }
    public decimal? Ind_3_3_N { get; set; }
    public decimal? Ind_3_3_R { get; set; }
    public string? Ind_3_3_Mech { get; set; }

    public decimal? Ind_3_4_D { get; set; }
    public decimal? Ind_3_4_R { get; set; }
    public string? Ind_3_4_Mech { get; set; }

    public decimal? Ind_4_4_D { get; set; }
    public decimal? Ind_4_4_N { get; set; }
    public decimal? Ind_4_4_R { get; set; }
    public string? Ind_4_4_Mech { get; set; }

    public decimal? Ind_4_5_D { get; set; }
    public decimal? Ind_4_5_R { get; set; }
    public string? Ind_4_5_Mech { get; set; }

    public decimal? Ind_5_1_D { get; set; }
    public decimal? Ind_5_1_R { get; set; }
    public string? Ind_5_1_Mech { get; set; }

    public decimal? Ind_5_3_D { get; set; }
    public decimal? Ind_5_3_N { get; set; }
    public decimal? Ind_5_3_R { get; set; }
    public string? Ind_5_3_Mech { get; set; }

    public decimal? Ind_6_1_D { get; set; }
    public decimal? Ind_6_1_R { get; set; }
    public string? Ind_6_1_Mech { get; set; }

    public decimal? Ind_6_2_D { get; set; }
    public decimal? Ind_6_2_R { get; set; }
    public string? Ind_6_2_Mech { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string? UserFullName { get; set; }
    public string? UserType { get; set; }
    public string? CollegeName { get; set; }
}
