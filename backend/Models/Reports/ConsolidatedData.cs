namespace OBFSimple.Models;

public class ConsolidatedData
{
    public string SubmissionMonth { get; set; } = "";
    public int CollegeCount { get; set; }

    public ConsolidatedKpi Ind_2_5 { get; set; } = new();
    public ConsolidatedKpi Ind_3_1 { get; set; } = new();
    public ConsolidatedKpi Ind_3_3 { get; set; } = new();
    public ConsolidatedKpi Ind_3_4 { get; set; } = new();
    public ConsolidatedKpi Ind_4_4 { get; set; } = new();
    public ConsolidatedKpi Ind_4_5 { get; set; } = new();
    public ConsolidatedKpi Ind_5_1 { get; set; } = new();
    public ConsolidatedKpi Ind_6_1 { get; set; } = new();
    public ConsolidatedKpi Ind_6_2 { get; set; } = new();
    public ConsolidatedKpi? Ind_5_3 { get; set; }
    public ConsolidatedKpi? UodInd_6_1 { get; set; }
    public ConsolidatedKpi? UodInd_6_2 { get; set; }
    public ConsolidatedKpi? ResearchInd_4_4 { get; set; }
    public ConsolidatedKpi? ResearchInd_4_5 { get; set; }
}
