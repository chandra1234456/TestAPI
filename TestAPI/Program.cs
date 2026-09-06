

using Microsoft.EntityFrameworkCore;

// Enable Npgsql legacy timestamp behavior for PostgreSQL timestamp compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configuration for cross-origin Dashboard/SDK testing
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ✅ Register DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

builder.Services.AddDbContext<TestAPI.AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Kestrel for Render (uses PORT env var, defaults to 5000)
var portEnv = Environment.GetEnvironmentVariable("PORT");
int port = int.TryParse(portEnv, out var p) ? p : 5000;

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

var app = builder.Build();

// Enable CORS
app.UseCors("AllowAll");

// Serve Static Files (if any, e.g. APKs)
app.UseStaticFiles();

// Auto-create/ensure DB schema on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<TestAPI.AppDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating/connecting DB: {ex.Message}");
    }
}

// 📌 Request Logging Middleware: Automatically records all API calls in Supabase DB (sdk_networks)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // Skip swagger documentation internal assets
    if (path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".png") || path.EndsWith(".ico") || path == "/swagger/v1/swagger.json")
    {
        await next();
        return;
    }

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    Exception? requestException = null;

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        requestException = ex;
        throw;
    }
    finally
    {
        stopwatch.Stop();
        try
        {
            using var logScope = context.RequestServices.CreateScope();
            var db = logScope.ServiceProvider.GetRequiredService<TestAPI.AppDbContext>();

            var networkLog = new TestAPI.Models.SdkNetwork
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = "api_server",
                SessionId = "server_session",
                Method = context.Request.Method,
                Url = $"{context.Request.Path}{context.Request.QueryString}",
                StatusCode = context.Response.StatusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = requestException?.Message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CreatedUtc = DateTime.UtcNow
            };

            db.SdkNetworks.Add(networkLog);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging API request to Supabase: {ex.Message}");
        }
    }
});

// Enable Swagger always (served at application root /)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Debug SDK API v1");
    c.RoutePrefix = string.Empty; // Serves Swagger UI directly at root (/)
});

app.UseAuthorization();

app.MapControllers();

app.Run();

