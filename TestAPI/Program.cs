var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Kestrel for HTTP only on port 5000
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP
});

var app = builder.Build();

// Enable Swagger UI always (not just in Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestAPI v1");
});

// If you want to enable HTTPS redirection, uncomment below (usually your host handles it)
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
