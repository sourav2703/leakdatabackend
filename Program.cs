// Program.cs
using Microsoft.EntityFrameworkCore;
using Npgsql;
using YourApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Supabase connection with better settings
var connectionString = builder.Configuration.GetConnectionString("Supabase") ??
    throw new InvalidOperationException("Supabase connection string not configured");

// Configure DbContext with retry and timeout settings
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:4200",
                "https://your-frontend-domain.onrender.com" // Add your frontend URL
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});

builder.Services.AddScoped<LeakOsintService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularApp");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Apply migrations with better error handling
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Migration failed: {ex.Message}");
    // Continue anyway - the app might work with existing schema
}

app.Run();