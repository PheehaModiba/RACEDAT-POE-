namespace RaceDay.Web.Models;

// Shapes of the JSON returned by the Part 2 API. Property names are matched
// case-insensitively, so "eventType" in JSON binds to EventType here.
// If your API uses different names, change them HERE (one place).

public class LoginResponse
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Role { get; set; } = "";
    public string? Token { get; set; }   // only if your API issues a token as well as a session
}

public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "Run";   // Run | Walk | Cycle
    public string? BannerUrl { get; set; }           // Azure Blob URL stored by the API
    public int EnrolmentCount { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
}

public class EnrolmentDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime EnrolledAt { get; set; }
}

public class ResultDto
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string ParticipantName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string FinishTime { get; set; } = "";     // "01:23:45"
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}

public class ProfileDto
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
