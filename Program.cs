using Microsoft.EntityFrameworkCore;
using Npgsql;
using YourApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// LOAD CONFIGURATION - Make appsettings.json optional
// ============================================
var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)  // Changed to optional
    .AddEnvironmentVariables();  // Environment variables take precedence

var configuration = configurationBuilder.Build();

// ============================================
// LOAD SECRETS FROM ENVIRONMENT VARIABLES
// ============================================

// Read from environment variables first, fallback to config
var leakOsintToken = Environment.GetEnvironmentVariable("LEAKOSINT_TOKEN") ??
                    configuration["LeakOsint:Token"];

var leakOsintApiUrl = Environment.GetEnvironmentVariable("LEAKOSINT_API_URL") ??
                     configuration["LeakOsint:ApiUrl"] ??
                     "https://leakosintapi.com/";

var supabaseConnectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING") ??
                               configuration.GetConnectionString("Supabase");

// ============================================
// VALIDATE REQUIRED CONFIGURATION
// ============================================
if (string.IsNullOrEmpty(supabaseConnectionString))
{
    Console.WriteLine("⚠️ WARNING: SUPABASE_CONNECTION_STRING environment variable is not set!");
    Console.WriteLine("⚠️ The application will not function correctly without it.");
}

if (string.IsNullOrEmpty(leakOsintToken))
{
    Console.WriteLine("⚠️ WARNING: LEAKOSINT_TOKEN environment variable is not set!");
    Console.WriteLine("⚠️ The application will not function correctly without it.");
}

// Override builder configuration with environment variables
if (!string.IsNullOrEmpty(supabaseConnectionString))
{
    builder.Configuration["ConnectionStrings:Supabase"] = supabaseConnectionString;
}

if (!string.IsNullOrEmpty(leakOsintToken))
{
    builder.Configuration["LeakOsint:Token"] = leakOsintToken;
}

if (!string.IsNullOrEmpty(leakOsintApiUrl))
{
    builder.Configuration["LeakOsint:ApiUrl"] = leakOsintApiUrl;
}

// ============================================
// ADD CORS - MUST BE BEFORE ANY OTHER SERVICES
// ============================================
builder.Services.AddCors(options =>
{
    // For development - allow all
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // For production - specific origins
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "http://localhost:3000",
            "https://leak-data-ocean.netlify.app",
            "https://*.netlify.app",
            "https://your-frontend-domain.onrender.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// ============================================
// ADD SERVICES
// ============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================
// ADD HTTP CONTEXT ACCESSOR (for IP tracking)
// ============================================
builder.Services.AddHttpContextAccessor();

// ============================================
// CONFIGURE DBCONTEXT
// ============================================
if (string.IsNullOrEmpty(supabaseConnectionString))
{
    throw new InvalidOperationException(
        "Supabase connection string not configured. " +
        "Please set the SUPABASE_CONNECTION_STRING environment variable."
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(supabaseConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(60);
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

// ============================================
// CONFIGURE HTTP CLIENT
// ============================================
builder.Services.AddHttpClient<LeakOsintService>(client =>
{
    client.BaseAddress = new Uri(leakOsintApiUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "LeakOSINT-App/1.0");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// ============================================
// REGISTER SERVICES
// ============================================
builder.Services.AddScoped<LeakOsintService>();

// ============================================
// BUILD APP
// ============================================
var app = builder.Build();

// ============================================
// USE CORS - IMPORTANT: Must be BEFORE UseAuthorization and MapControllers
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowSpecificOrigins");
}

// ============================================
// CONFIGURE PIPELINE
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ============================================
// HEALTH CHECK ENDPOINT
// ============================================
app.MapGet("/health", async (IServiceProvider services) =>
{
    try
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();

        return Results.Ok(new
        {
            status = canConnect ? "healthy" : "unhealthy",
            database = canConnect ? "connected" : "disconnected",
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName
        });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { status = "error", error = ex.Message },
            statusCode: 500
        );
    }
});

// ============================================
// CLIENT INFO ENDPOINT (for debugging IP tracking)
// ============================================
app.MapGet("/client-info", (HttpContext context) =>
{
    var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    var clientIP = "Unknown";

    if (!string.IsNullOrEmpty(forwardedHeader))
    {
        clientIP = forwardedHeader.Split(',')[0].Trim();
    }
    else
    {
        var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIP))
        {
            clientIP = realIP;
        }
        else
        {
            clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

    return Results.Ok(new
    {
        ip = clientIP,
        userAgent = context.Request.Headers["User-Agent"].ToString(),
        forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString(),
        realIP = context.Request.Headers["X-Real-IP"].ToString(),
        remoteIP = context.Connection.RemoteIpAddress?.ToString(),
        timestamp = DateTime.UtcNow
    });
});

app.Run();