using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using JobHuntly.Api.Controllers;
using JobHuntly.Api.Infrastructure;
using JobHuntly.Application;
using JobHuntly.Application.Ai;
using JobHuntly.Infrastructure;
using JobHuntly.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Local development: export values from `.env` before configuration is built.
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums travel as readable strings ("Interviewing", "Technical"), not numbers.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Validation failures are 422 problem details, not 400.
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation failed"
            };
            return new UnprocessableEntityObjectResult(problem);
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Daily AI allowance per user (registered / demo sandbox). Optional: Ai__DailyLimit, Ai__DemoDailyLimit.
builder.Services.AddSingleton(new AiLimits(
    builder.Configuration.GetValue("Ai:DailyLimit", 30),
    builder.Configuration.GetValue("Ai:DemoDailyLimit", 8)));

// CORS: only the configured frontend origins. In production the browser talks to the frontend
// origin and the /api/* rewrite forwards here, so this is mostly relevant for local tooling.
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

// The client IP arrives in X-Forwarded-For (Vercel rewrite, then Fly proxy). Trusting it lets the
// rate limiter tell visitors apart; the trade-off is documented in docs/02-plan.md (D8).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = null;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Rate limiting on credential endpoints.
var authPerMinute = builder.Configuration.GetValue("RateLimit:AuthPerMinute", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "Please wait a minute and try again."
        }, ct);
    };
});

// JWT authentication, read from the HttpOnly cookie (or an Authorization header for tooling).
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("Secret");
if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
    throw new InvalidOperationException(
        "JwtSettings:Secret must be set to a random string of at least 32 characters (see .env.example).");

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
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue(AuthController.CookieName, out var token))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

// Apply EF Core migrations at startup (single instance; trade-off documented in the plan).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseMiddleware<CsrfGuardMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program;
