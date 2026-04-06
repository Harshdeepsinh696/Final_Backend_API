using Final_Backend_API.Data;
using Final_Backend_API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Smart Medicine Reminder API",
        Version = "v1"
    });
});

// ── Database (Auto Detect Local vs Render) ──
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<AppDbContext>(options =>
//{
//    if (!string.IsNullOrEmpty(connectionString) &&
//        connectionString.TrimStart().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
//    {
//        options.UseNpgsql(connectionString);
//        Console.WriteLine("✅ Using PostgreSQL (Render)");
//    }
//    else
//    {
//        options.UseSqlServer(connectionString);
//        Console.WriteLine("✅ Using SQL Server (Local)");
//    }
//});

// ── Database (Local + Render Support) ──
// ── Database (SQLite for Local + Render Free) ──
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=medicine.db");
    Console.WriteLine("✅ Using SQLite (Local + Render Free)");
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

// ── Middleware ──
app.UseRouting();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Medicine Reminder API v1");
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();


//add
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Migration error: " + ex.Message);
    }
}

app.Run();