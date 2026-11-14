using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Ok { get; init; }
    public T? Data { get; init; }
    public string Error { get; init; } = "";
    public HttpStatusCode Status { get; init; }
}

/// <summary>
/// The ONLY way the MVC app talks to the outside world: the Part 2 API.
/// It forwards the API's session cookie (and bearer token, if any) that we saved in
/// the MVC session at login, so the API knows who is calling and enforces their role.
/// Failures never throw to controllers - they come back as ApiResult with a friendly message.
/// </summary>
public class ApiClient
{
    private const string CookieKey = "RaceDay.ApiCookie";
    private const string TokenKey = "RaceDay.ApiToken";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;
    private readonly ILogger<ApiClient> _log;

    public ApiClient(HttpClient http, IHttpContextAccessor ctx, ILogger<ApiClient> log)
    {
        _http = http; _ctx = ctx; _log = log;
    }

    private ISession Session => _ctx.HttpContext!.Session;

    // ---- JSON helpers -------------------------------------------------------
    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, JsonBody(body));
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Put, url, JsonBody(body));
    public Task<ApiResult<T>> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url, null);

    private static StringContent? JsonBody(object? body) =>
        body == null ? null : new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

    // ---- Multipart (image uploads). The file goes MVC -> API -> Azure Blob Storage ----
    public async Task<ApiResult<T>> SendFormAsync<T>(HttpMethod method, string url,
        Dictionary<string, string?> fields, string fileField, IFormFile? file)
    {
        var form = new MultipartFormDataContent();
        foreach (var (k, v) in fields)
            if (v != null) form.Add(new StringContent(v), k);

        if (file is { Length: > 0 })
        {
            var stream = file.OpenReadStream();
            var part = new StreamContent(stream);
            part.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(part, fileField, file.FileName);
        }
        return await SendAsync<T>(method, url, form);
    }

    // ---- Core -----------------------------------------------------------------
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url.TrimStart('/')) { Content = content };

            var cookie = Session.GetString(CookieKey);
            if (!string.IsNullOrEmpty(cookie)) req.Headers.TryAddWithoutValidation("Cookie", cookie);
            var token = Session.GetString(TokenKey);
            if (!string.IsNullOrEmpty(token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var res = await _http.SendAsync(req);

            if (res.Headers.TryGetValues("Set-Cookie", out var setCookies))
                SaveCookies(setCookies);

            var body = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try { data = JsonSerializer.Deserialize<T>(body, Json); }
                    catch (JsonException ex) { _log.LogWarning(ex, "Could not read API response for {Url}", url); }
                }
                return new ApiResult<T> { Ok = true, Data = data, Status = res.StatusCode };
            }

            _log.LogWarning("API {Method} {Url} returned {Status}", method, url, (int)res.StatusCode);
            return new ApiResult<T> { Ok = false, Status = res.StatusCode, Error = FriendlyError(res.StatusCode, body) };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _log.LogError(ex, "API unreachable: {Url}", url);
            return new ApiResult<T>
            {
                Ok = false, Status = HttpStatusCode.ServiceUnavailable,
                Error = "We can't reach the RaceDay service right now. Please try again in a moment."
            };
        }
    }

    // Keep the API's session cookie so later calls are "logged in" as the same user.
    private void SaveCookies(IEnumerable<string> setCookieHeaders)
    {
        var jar = new Dictionary<string, string>();
        var existing = Session.GetString(CookieKey);
        if (!string.IsNullOrEmpty(existing))
            foreach (var pair in existing.Split("; ", StringSplitOptions.RemoveEmptyEntries))
            {
                var i = pair.IndexOf('=');
                if (i > 0) jar[pair[..i]] = pair[(i + 1)..];
            }

        foreach (var header in setCookies(setCookieHeaders))
            jar[header.name] = header.value;

        Session.SetString(CookieKey, string.Join("; ", jar.Select(kv => $"{kv.Key}={kv.Value}")));

        static IEnumerable<(string name, string value)> setCookies(IEnumerable<string> headers)
        {
            foreach (var h in headers)
            {
                var first = h.Split(';')[0];
                var i = first.IndexOf('=');
                if (i > 0) yield return (first[..i].Trim(), first[(i + 1)..].Trim());
            }
        }
    }

    public void SaveToken(string? token)
    {
        if (!string.IsNullOrEmpty(token)) Session.SetString(TokenKey, token);
    }

    public void ForgetApiSession()
    {
        Session.Remove(CookieKey);
        Session.Remove(TokenKey);
    }

    // Turn whatever the API said into a message that is safe to show a user.
    private static string FriendlyError(HttpStatusCode status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
                {
                    var msgs = errs.EnumerateObject()
                        .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray() : Enumerable.Empty<JsonElement>())
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!).Take(3).ToList();
                    if (msgs.Count > 0) return string.Join(" ", msgs);
                }
                foreach (var key in new[] { "message", "error", "detail", "title" })
                    if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(v.GetString()) && v.GetString()!.Length < 200)
                        return v.GetString()!;
            }
        }
        catch (JsonException) { /* not JSON - fall through to generic message */ }

        return status switch
        {
            HttpStatusCode.BadRequest => "Some of the details weren't valid. Please check and try again.",
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with something that already exists (for example, you may already be enrolled).",
            _ => "Something went wrong on our side. Please try again."
        };
    }
}
