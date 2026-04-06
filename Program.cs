using Final_Backend_API.Data;
using Final_Backend_API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database - HARDCODED SQLITE (ignores appsettings.json) ──
var dbPath = Path.Combine(AppContext.BaseDirectory, "medicine.db");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
    Console.WriteLine($"✅ Using SQLite at: {dbPath}");
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddHttpClient<SmsService>();
builder.Services.AddHostedService<MedicineReminderService>();

var app = builder.Build();

// Migrate DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("✅ Database migrated successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Migration error: " + ex.Message);
    }
}

// Middleware
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

// Swagger - always on
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Medicine Reminder API v1");
    c.RoutePrefix = "swagger";
});

// ✅ Root → redirects to swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// ✅ Health check - test this first!
app.MapGet("/health", () => Results.Ok(new
{
    status = "✅ Running",
    time = DateTime.UtcNow,
    swagger = "Visit /swagger"
}));

app.MapControllers();

// Port for Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");
Console.WriteLine($"🚀 Running on port {port}");

app.Run();