using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PrintGuard.API.Middleware;
using PrintGuard.Application.Interfaces;
using PrintGuard.Infrastructure.Data;
using PrintGuard.Infrastructure.Security;
using PrintGuard.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=printguard_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// Dependency Injection
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IPrintJobService, PrintJobService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "PrintGuardSecretKeyMustBe32CharactersOrMore_SuperSecure2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PrintGuardServer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PrintGuardClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PrintGuard API",
        Version = "v1",
        Description = "Print Management, Employee Authentication & Monitoring System API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PrintGuard API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto-create database & tables on startup if PostgreSQL is reachable
try
{
    var builderHelper = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
    var targetDb = builderHelper.Database;
    builderHelper.Database = "postgres";

    using var masterConn = new Npgsql.NpgsqlConnection(builderHelper.ConnectionString);
    await masterConn.OpenAsync();

    using var checkCmd = masterConn.CreateCommand();
    checkCmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{targetDb}'";
    var exists = await checkCmd.ExecuteScalarAsync();

    if (exists == null)
    {
        using var createCmd = masterConn.CreateCommand();
        createCmd.CommandText = $"CREATE DATABASE \"{targetDb}\"";
        await createCmd.ExecuteNonQueryAsync();
        Console.WriteLine($"[PrintGuard] Created database '{targetDb}' successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[PrintGuard] Master DB check: {ex.Message}");
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("[PrintGuard] Database schema verified.");

        // Auto-detect and register real physical Windows printers
        await WindowsPrinterDiscovery.SyncWithDatabaseAsync(db);

        // Check if seed data exists
        if (!await db.AdminUsers.AnyAsync())
        {
            var seedPath = Path.Combine(app.Environment.ContentRootPath, "..", "..", "database", "seeds", "seed_data.sql");
            if (File.Exists(seedPath))
            {
                var sql = await File.ReadAllTextAsync(seedPath);
                await db.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine("[PrintGuard] Seeded initial database records.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[PrintGuard] Database initialization error: {ex.Message}");
    }
}

app.Run();
