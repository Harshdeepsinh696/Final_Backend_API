using Final_Backend_API.Data;
using Final_Backend_API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ──
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Smart Medicine Reminder API",
        Version = "v1"
    });
});

// ── Database (SQLite - works on Local + Render Free) ──
var dbPath = Path.Combine(AppContext.BaseDirectory, "medicine.db");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
    Console.WriteLine($"✅ Using SQLite at: {dbPath}");
});

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ── SMS Service ──
builder.Services.AddHttpClient<SmsService>();

// ── Background Reminder Service ──
builder.Services.AddHostedService<MedicineReminderService>();

var app = builder.Build();

// ── Migrate Database ──
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

// ── Middleware (Order matters!) ──
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthorization();

// ── Swagger UI (always enabled, even in production on Render) ──
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Medicine Reminder API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

// ── Port Binding for Render ──
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");
Console.WriteLine($"🚀 Starting on port {port}");

app.Run();