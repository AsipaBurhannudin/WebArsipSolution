using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebArsip.Infrastructure.DbContexts;
using WebArsip.Mvc.Handlers;
using WebArsip.Mvc.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔹 MVC + NewtonsoftJson
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });

// 🔹 Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

// 🔹 Authentication (Cookie)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Error/AccessDenied";
    });

// 🔹 HttpClient (API)
builder.Services.AddHttpClient("WebArsipApi", client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(apiBaseUrl!);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddTransient<JwtAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 🔹 Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Session aktif duluan
app.UseSession();

// ✅ Authentication & Authorization sebelum middleware custom
app.UseAuthentication();
app.UseAuthorization();

// ✅ Custom middleware role-check
app.UseMiddleware<RoleAccessMiddleware>();

// 🔹 Routing default
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();