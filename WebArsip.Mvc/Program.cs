using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using WebArsip.Mvc.Handlers;

var builder = WebApplication.CreateBuilder(args);

// MVC + NewtonsoftJson
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        // opsi lain kalau perlu:
        // options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// HttpClient untuk API + Add Message Handler
builder.Services.AddTransient<JwtAuthorizationHandler>();

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

builder.Services.AddHttpClient("WebArsipApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl!);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();