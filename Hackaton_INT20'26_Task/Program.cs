using Application;
using Application.Repositories.Implementations;
using Application.Repositories.Interfaces;
using Application.Seeders;
using Domain.Model.Auth;
using Domain.Services;
using DotNetEnv;
using Infrastructure.Extensions;
using Infrastructure.Services;
using Infrastructure.Services.Implementations;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load .env → environment variables → IConfiguration
var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envPath))
    Env.Load(envPath);

builder.Configuration.AddEnvironmentVariables();

// Port (from .env PORT=...)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// Clock — set FAKE_DATE=2025-11-15T10:00:00Z to freeze time for manual testing
var fakeDateRaw = builder.Configuration["FAKE_DATE"];
if (fakeDateRaw is not null && DateTime.TryParse(fakeDateRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var fakeDate))
    builder.Services.AddSingleton<IDateTimeProvider>(new FakeDateTimeProvider(fakeDate));
else
    builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection") + ";Maximum Pool Size=15;",
        npgsql => npgsql.MigrationsAssembly("Application")
    ));

// Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit           = true;
        options.Password.RequiredLength         = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase       = false;
        options.User.RequireUniqueEmail         = true;
        options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers      = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Identity cookie auth (no JWT — cookie is managed by SignInManager)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name       = "auth";
    options.Cookie.HttpOnly   = true;
    options.Cookie.SameSite = builder.Environment.IsDevelopment()
        ? SameSiteMode.Lax
        : SameSiteMode.None;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.SlidingExpiration  = true;
    options.ExpireTimeSpan     = TimeSpan.FromDays(14);

    // Return 401/403 JSON instead of redirecting to a login page
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// CORS (from .env CORS_ORIGINS=http://localhost:3000,http://localhost:5173)

Console.WriteLine("CORS: " + Environment.GetEnvironmentVariable("CORS_ORIGINS"));

var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ORIGINS") ?? "http://localhost:3000,http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ClaudeService>();
builder.Services.AddInfrastructure();
builder.Services.AddClaudeService(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();
builder.Services.AddHostedService<SessionAbandonmentService>();
builder.Services.AddScoped<INodeRepository, NodeRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<IEdgeRepository, EdgeRepository>();
builder.Services.AddScoped<IFlowRepository, FlowRepository>();
builder.Services.AddScoped<IFlowStatsQueryService, FlowStatsQueryService>();
builder.Services.AddScoped<INodeOfferRepository, NodeOfferRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IOptionRepository, OptionRepository>();
builder.Services.AddScoped<ISessionOfferRepository, SessionOfferRepository>();
builder.Services.AddScoped<IUserAnswerRepository, UserAnswerRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<INodeRedirectLinkRepository, NodeRedirectLinkRepository>();
builder.Services.AddScoped<INodeLeadCaptureFieldRepository, NodeLeadCaptureFieldRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();



builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Hackaton INT20'26 API",
        Version     = "v1",
        Description = "Auth is cookie-based: call POST /api/auth/login to receive the access_token cookie."
    });

    options.EnableAnnotations();

    options.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Name        = "auth",
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Cookie,
        Description = "HttpOnly Identity cookie. Call POST /api/auth/login first, then use Try it out."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "cookieAuth" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hackaton INT20'26 v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate on startup (dev only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();                  // create tables first
    await DbSeeder.SeedAsync(scope.ServiceProvider);   // then seed data
}
app.Run();
