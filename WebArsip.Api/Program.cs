using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;
using WebArsip.Infrastructure.Services;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ✅ Database Context
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<AuditLogService>();
        builder.Services.AddScoped<PermissionService, PermissionService>();
        builder.Services.AddScoped<SerialNumberService>();
        builder.Services.AddScoped<SerialNumberMonthlyCounter>();

        builder.Services.AddMemoryCache();
        builder.Services.AddLogging();

        // ✅ Identity
        builder.Services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ✅ JWT Authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["Key"]))
            };
        });

        builder.Services.AddAuthorization();

        // ✅ Controllers + Swagger + JWT Support
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebArsip API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Masukkan JWT token dengan format: Bearer {token}",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new List<string>()
                }
            });
        });

        // ✅ Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // ✅ CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowNgrok", policy =>
                policy.WithOrigins(
                    "https://nongrounded-nonrefuelling-kizzy.ngrok-free.dev",
                    "http://localhost:5077",
                    "https://localhost:7237"
                )
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        // ---- BUILD APPLICATION ----
        var app = builder.Build();

        // ✅ Seed default roles & admin
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            string[] roles = { "Admin", "Compliance", "Audit", "Policy" };

            foreach (var role in roles)
            {
                if (!roleManager.RoleExistsAsync(role).Result)
                    roleManager.CreateAsync(new Role { Name = role }).Wait();
            }

            var adminEmail = "admin@company.com";
            var adminUser = userManager.FindByEmailAsync(adminEmail).Result;

            if (adminUser == null)
            {
                var user = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = userManager.CreateAsync(user, "Admin@123").Result;
                if (result.Succeeded)
                    userManager.AddToRoleAsync(user, "Admin").Wait();

                var adminRole = roleManager.FindByNameAsync("Admin").Result;
                if (adminRole != null && !context.Permissions.Any(p => p.RoleId == adminRole.Id))
                {
                    var documents = context.Documents.ToList();
                    foreach (var doc in documents)
                    {
                        context.Permissions.Add(new Permission
                        {
                            RoleId = adminRole.Id,
                            CanView = true,
                            CanEdit = true,
                            CanUpload = true,
                            CanDelete = true
                        });
                    }
                    context.SaveChanges();
                }
            }
        }

        // ---- PIPELINE ----
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseCors("AllowNgrok");

        app.UseAuthentication();
        app.UseAuthorization();

        // Static files for upload
        var storageRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
        var storagePath = Path.Combine(storageRoot, "WebArsipStorage", "uploads");

        // 🛠️ FIX: Auto-create folder saat test / production
        if (!Directory.Exists(storagePath))
        {
            Directory.CreateDirectory(storagePath);
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(storagePath),
            RequestPath = "/uploads"
        });

        app.MapControllers();

        // Redirect 302 → 401 handler
        app.Use(async (context, next) =>
        {
            if (context.Response.StatusCode == 302 &&
                context.Response.Headers.ContainsKey("Location") &&
                context.Response.Headers["Location"].ToString().Contains("/Account/Login"))
            {
                context.Response.StatusCode = 401;
                return;
            }

            await next();
        });

        app.Run();
    }
}