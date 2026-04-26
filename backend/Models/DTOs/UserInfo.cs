namespace OBFSimple.Models;

public class UserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public string? UserType { get; set; }
    public string? CollegeName { get; set; }
}
