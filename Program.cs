using Microsoft.EntityFrameworkCore;
using Npgsql;
using YourApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// LOAD SECRETS FROM ENVIRONMENT VARIABLES
// ============================================

// Read from environment variables first, fallback to config
var leakOsintToken = Environment.GetEnvironmentVariable("LEAKOSINT_TOKEN") ??
                    builder.Configuration["LeakOsint:Token"];

var supabaseConnectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING") ??
                               builder.Configuration.GetConnectionString("Supabase");

// Build configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Update connection string with environment variable if available
if (!string.IsNullOrEmpty(supabaseConnectionString))
{
    // Replace placeholder or use directly
    builder.Configuration["ConnectionStrings:Supabase"] = supabaseConnectionString;
}

if (!string.IsNullOrEmpty(leakOsintToken))
{
    builder.Configuration["LeakOsint:Token"] = leakOsintToken;
}

// ============================================
// REST OF YOUR PROGRAM.CS CODE
// ============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext
var connectionString = builder.Configuration.GetConnectionString("Supabase") ??
    throw new InvalidOperationException("Supabase connection string not configured");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(60);
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

// Configure HttpClient
builder.Services.AddHttpClient<LeakOsintService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["LeakOsint:ApiUrl"] ?? "https://leakosintapi.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "LeakOSINT-App/1.0");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:4200",
                "https://your-frontend-domain.onrender.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        });
});

builder.Services.AddScoped<LeakOsintService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularApp");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check
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
            timestamp = DateTime.UtcNow
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

app.Run();