using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginVm
{
    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "That doesn't look like a valid email.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class RegisterVm
{
    [Required(ErrorMessage = "First name is required."), StringLength(60)]
    public string FirstName { get; set; } = "";
    [Required(ErrorMessage = "Last name is required."), StringLength(60)]
    public string LastName { get; set; } = "";
    [Required, EmailAddress(ErrorMessage = "That doesn't look like a valid email.")]
    public string Email { get; set; } = "";
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }
    [Required, MinLength(8, ErrorMessage = "Use at least 8 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
    [Required(ErrorMessage = "Choose a role.")]
    public string Role { get; set; } = "Participant";
}

public class EventListVm
{
    public List<EventDto> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public decimal? MaxDistance { get; set; }
}

public class EventDetailsVm
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public bool AlreadyEnrolled { get; set; }
}

public class EnrolVm
{
    public int EventId { get; set; }
    [Required(ErrorMessage = "Please choose a category.")]
    public int? CategoryId { get; set; }
}

public class EventFormVm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Give the event a name."), StringLength(120)]
    public string Name { get; set; } = "";
    [Required(ErrorMessage = "Add a short description."), StringLength(2000)]
    public string Description { get; set; } = "";
    [Required(ErrorMessage = "Pick a date and start time."), DataType(DataType.DateTime)]
    public DateTime? Date { get; set; }
    [Required(ErrorMessage = "Where is it happening?"), StringLength(200)]
    public string Location { get; set; } = "";
    [Required(ErrorMessage = "Enter the distance."), Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    public decimal? DistanceKm { get; set; }
    [Required(ErrorMessage = "Choose Run, Walk or Cycle.")]
    public string EventType { get; set; } = "Run";
    public string? ExistingBannerUrl { get; set; }
    public IFormFile? Banner { get; set; }
}

public class DashboardVm
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public List<EventDto> Upcoming => Events.Where(e => e.Date >= DateTime.Now).OrderBy(e => e.Date).Take(5).ToList();
}

public class CategoriesVm
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    [Required(ErrorMessage = "Enter a category name, e.g. 10km, Under 20, Senior."), StringLength(60)]
    public string NewName { get; set; } = "";
}

public class EventEnrolmentsVm
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRowVm
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    [RegularExpression(@"^\d{1,3}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use hh:mm:ss, e.g. 01:23:45.")]
    public string? FinishTime { get; set; }
    [Range(1, 100000, ErrorMessage = "Position must be 1 or more.")]
    public int? Position { get; set; }
}

public class ResultsEntryVm
{
    public EventDto Event { get; set; } = new();
    public List<ResultRowVm> Rows { get; set; } = new();
}

public class ProfileVm
{
    [Required, StringLength(60)] public string FirstName { get; set; } = "";
    [Required, StringLength(60)] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Phone(ErrorMessage = "Enter a valid phone number.")] public string? Phone { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public IFormFile? Picture { get; set; }
}
