using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OBFSimple.Models;
using OBFSimple.Services;

namespace OBFSimple.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataController : ControllerBase
{
    private readonly ObfDataService _data;
    private readonly UserService _users;
    private readonly ConsolidatedService _consolidated;

    public DataController(ObfDataService data, UserService users, ConsolidatedService consolidated)
    {
        _data = data;
        _users = users;
        _consolidated = consolidated;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    private string GetUsername() => User.FindFirst(ClaimTypes.Name)?.Value ?? "";
    private bool IsAdmin() => User.FindFirst("Role")?.Value == "admin";

    private string GetCollegeKey()
    {
        var u = new User
        {
            UserType = User.FindFirst("UserType")?.Value,
            CollegeName = User.FindFirst("CollegeName")?.Value
        };
        if (string.IsNullOrEmpty(u.CollegeKey))
            throw new InvalidOperationException($"Cannot determine college key for UserType: {u.UserType ?? "null"}");
        return u.CollegeKey;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(IsAdmin() ? _data.GetAllData(page, pageSize) : _data.GetDataByCollegeKey(GetCollegeKey(), page, pageSize));

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var record = _data.GetDataById(id);
        if (record == null) return NotFound(new { message = "Record not found" });
        if (!IsAdmin() && record.CollegeKey != GetCollegeKey()) return Forbid();
        return Ok(record);
    }

    [HttpGet("consolidated/{year:int}-{month:int}")]
    public IActionResult GetConsolidated(int year, int month)
    {
        if (!IsAdmin()) return Forbid();
        return Ok(_consolidated.GetConsolidatedData($"{year:D4}-{month:D2}"));
    }

    [HttpGet("/api/users")]
    public IActionResult GetUsers() => IsAdmin() ? Ok(_users.GetAllUsers()) : Forbid();

    [HttpPost]
    public IActionResult Create([FromBody] OBFData data)
    {
        var collegeKey = GetCollegeKey();
        if (string.IsNullOrEmpty(collegeKey))
            return BadRequest(new { message = "Your account is not configured for data submission." });

        data.SubmissionMonth = DateTime.Now.ToString("yyyy-MM");

        var error = _data.ValidateData(data);
        if (error != null) return BadRequest(new { message = error });

        if (_data.CollegeMonthExists(collegeKey, data.SubmissionMonth))
            return Conflict(new { message = $"Your college already has a submission for {data.SubmissionMonth}. Please edit the existing record instead." });

        var id = _data.CreateData(data, GetUserId(), GetUsername(), collegeKey);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] OBFData data)
    {
        var existing = _data.GetDataById(id);
        if (existing == null) return NotFound(new { message = "Record not found" });

        data.SubmissionMonth = existing.SubmissionMonth;

        var error = _data.ValidateData(data);
        if (error != null) return BadRequest(new { message = error });

        var collegeKey = GetCollegeKey();
        if (!IsAdmin() && _data.CollegeMonthExists(collegeKey, data.SubmissionMonth, id))
            return Conflict(new { message = $"Your college already has a submission for {data.SubmissionMonth}." });

        var success = _data.UpdateData(id, data, GetUsername(), collegeKey, IsAdmin());
        return success ? Ok(new { message = "Updated successfully" }) : NotFound(new { message = "Record not found or access denied" });
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var success = _data.DeleteData(id, GetCollegeKey(), IsAdmin());
        return success ? Ok(new { message = "Deleted successfully" }) : NotFound(new { message = "Record not found or access denied" });
    }
}
