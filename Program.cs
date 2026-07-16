using Microsoft.EntityFrameworkCore;
using Npgsql;
using YourApp.Services;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// FORCE IPv4 FOR ALL NETWORK CONNECTIONS
// This is the critical fix for Render + Supabase
// ============================================
ServicePointManager.DnsRefreshTimeout = 0;
AppContext.SetSwitch("System.Net.Dns.EnableDnsResolutionCache", false);

// Try to pre-resolve and cache the IPv4 address
string ipv4Host = null;
try
{
    var hostEntry = Dns.GetHostEntry("db.wptsgoswwqoazcbbrkdh.supabase.co");
    var ipv4Address = hostEntry.AddressList
        .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

    if (ipv4Address != null)
    {
        ipv4Host = ipv4Address.ToString();
        Console.WriteLine($"✅ Resolved IPv4 address: {ipv4Host}");
    }
    else
    {
        Console.WriteLine("⚠️ No IPv4 address found, will use hostname");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Failed to resolve IPv4: {ex.Message}");
}

// ============================================
// BUILD CONNECTION STRING WITH IPv4
// ============================================
var originalConnectionString = builder.Configuration.GetConnectionString("Supabase") ??
    throw new InvalidOperationException("Supabase connection string not configured");

// If we have an IPv4 address, use it directly
string connectionString;
if (!string.IsNullOrEmpty(ipv4Host))
{
    // Replace the host with the IPv4 address
    connectionString = originalConnectionString
        .Replace("Host=db.wptsgoswwqoazcbbrkdh.supabase.co;", $"Host={ipv4Host};")
        .Replace("Host=db.wptsgoswwqoazcbbrkdh.supabase.co", $"Host={ipv4Host}");

    Console.WriteLine($"✅ Using IPv4 connection string");
}
else
{
    // Fallback: Try with different host patterns
    connectionString = originalConnectionString;
    Console.WriteLine($"⚠️ Using original connection string (may fail)");
}

// Log the connection string (mask password)
var maskedConnectionString = connectionString
    .Replace($"Password={builder.Configuration.GetConnectionString("Supabase")?.Split(';').FirstOrDefault(s => s.Contains("Password="))?.Split('=')[1]}", "Password=***");
Console.WriteLine($"📡 Connection string: {maskedConnectionString}");

// ============================================
// ADD SERVICES
// ============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================
// CONFIGURE DbContext WITH RETRY AND IPv4
// ============================================
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enable retry for transient failures
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);

        // Command timeout
        npgsqlOptions.CommandTimeout(60);

        // Split query behavior for complex queries
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

// ============================================
// CONFIGURE HTTP CLIENT
// ============================================
builder.Services.AddHttpClient<LeakOsintService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["LeakOsint:ApiUrl"] ?? "https://leakosintapi.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "LeakOSINT-App/1.0");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// ============================================
// CORS CONFIGURATION
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:4200",
                "https://your-frontend-domain.onrender.com" // Replace with your actual frontend URL
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        });
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
// CONFIGURE PIPELINE
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularApp");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ============================================
// DATABASE MIGRATION WITH ERROR HANDLING
// ============================================
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Test the connection first
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            Console.WriteLine("✅ Database connection successful!");

            // Apply migrations
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("✅ Migrations applied successfully!");
        }
        else
        {
            Console.WriteLine("❌ Cannot connect to database. Check your connection string.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Migration/Connection error: {ex.Message}");
    Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
    // Continue anyway - the app might work with existing schema
}

// ============================================
// ADD FALLBACK HEALTH CHECK ENDPOINT
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
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        // Return a 500 status code with a simple object
        return Results.StatusCode(500);
    }
});

app.Run();