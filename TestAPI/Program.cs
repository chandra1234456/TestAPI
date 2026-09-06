

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

// ✅ Register DbContext (prioritize DATABASE_URL from Render, fallback to appsettings)
var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "";

var connectionString = ParseConnectionString(rawConnectionString);

builder.Services.AddDbContext<TestAPI.AppDbContext>(options =>
    options.UseNpgsql(connectionString));

static string ParseConnectionString(string connStr)
{
    if (string.IsNullOrWhiteSpace(connStr)) return connStr;
    if (connStr.StartsWith("postgres://") || connStr.StartsWith("postgresql://"))
    {
        try
        {
            var uri = new Uri(connStr);
            var userInfo = uri.UserInfo.Split(':');
            var user = Uri.UnescapeDataString(userInfo[0]);
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var db = uri.AbsolutePath.TrimStart('/');

            // Supabase IPv4 / IPv6 dual-stack pooler rewrite for Render deployment
            if (host.Contains("supabase.co") || host.Contains("cxxugsvxwkmlvcegkhkg"))
            {
                host = "aws-0-ap-southeast-1.pooler.supabase.com";
                if (!user.Contains("."))
                {
                    user = $"{user}.cxxugsvxwkmlvcegkhkg";
                }
            }

            return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
        }
        catch
        {
            return connStr;
        }
    }

    if (connStr.Contains("db.cxxugsvxwkmlvcegkhkg.supabase.co"))
    {
        connStr = connStr.Replace("db.cxxugsvxwkmlvcegkhkg.supabase.co", "aws-0-ap-southeast-1.pooler.supabase.com");
        if (connStr.Contains("Username=postgres;") || connStr.Contains("User Id=postgres;"))
        {
            connStr = connStr.Replace("Username=postgres;", "Username=postgres.cxxugsvxwkmlvcegkhkg;")
                             .Replace("User Id=postgres;", "User Id=postgres.cxxugsvxwkmlvcegkhkg;");
        }
    }

    return connStr;
}

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
        Console.WriteLine("✅ Supabase database connected successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating/connecting DB: {ex.Message}");
    }
}

// 📌 Request Logging Middleware: Automatically records API calls in Supabase DB (sdk_networks)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // Skip swagger documentation internal assets & static assets
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
        var method = context.Request.Method;
        var fullUrl = $"{context.Request.Path}{context.Request.QueryString}";
        var statusCode = context.Response.StatusCode;
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var errMsg = requestException?.Message;

        // Async background write so request execution is never blocked
        _ = Task.Run(async () =>
        {
            try
            {
                using var logScope = app.Services.CreateScope();
                var db = logScope.ServiceProvider.GetRequiredService<TestAPI.AppDbContext>();

                var networkLog = new TestAPI.Models.SdkNetwork
                {
                    Id = Guid.NewGuid().ToString(),
                    ProjectId = "api_server",
                    SessionId = "server_session",
                    Method = method,
                    Url = fullUrl,
                    StatusCode = statusCode,
                    DurationMs = elapsedMs,
                    ErrorMessage = errMsg,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    CreatedUtc = DateTime.UtcNow
                };

                db.SdkNetworks.Add(networkLog);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background API request log error: {ex.Message}");
            }
        });
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

