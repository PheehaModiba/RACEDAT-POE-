using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Session: MVC keeps the logged-in user (id + role) and the API's session cookie/token here.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".RaceDay.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromMinutes(60);
});

// One typed HttpClient for the Part 2 API. MVC never talks to the DB or Azure directly.
// Api:BaseUrl can be overridden with the env var Api__BaseUrl (used in Docker).
var apiBase = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000/";
builder.Services.AddHttpClient<ApiClient>(c =>
{
    c.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
    c.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    // UseCookies = false: we attach the API's session cookie ourselves, per MVC user.
    var h = new HttpClientHandler { UseCookies = false };
    if (builder.Environment.IsDevelopment())
        h.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    return h;
});

var app = builder.Build();

// Users never see raw exceptions or status pages.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
