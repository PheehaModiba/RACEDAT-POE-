using System.Text.Json;

namespace RaceDay.Web.Services;

/// <summary>What we remember about the logged-in user for every page.</summary>
public class SessionUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsOrganiser => Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase);
    public bool IsParticipant => Role.Equals("Participant", StringComparison.OrdinalIgnoreCase);
}

public static class SessionExtensions
{
    private const string UserKey = "RaceDay.User";

    public static void SetUser(this ISession s, SessionUser u) =>
        s.SetString(UserKey, JsonSerializer.Serialize(u));

    public static SessionUser? GetUser(this ISession s)
    {
        var json = s.GetString(UserKey);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<SessionUser>(json);
    }
}
