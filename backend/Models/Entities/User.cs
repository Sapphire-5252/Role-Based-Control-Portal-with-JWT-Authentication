namespace OBFSimple.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "standard";
    public string? UserType { get; set; }
    public string? CollegeName { get; set; }

    public string CollegeKey => UserType switch
    {
        "cgs" => "CGS",
        "uod" => "UOD",
        "research_office" => "Research Office",
        "college" => CollegeName ?? "",
        _ => ""
    };
}
