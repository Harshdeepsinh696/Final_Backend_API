using Final_Backend_API.Data;
using Final_Backend_API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database (Auto-detects Local vs Production) ──
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var isPostgres = connectionString != null && connectionString.TrimStart().StartsWith("Host=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isPostgres)
    {
        options.UseNpgsql(connectionString);
        Console.WriteLine("✅ Using PostgreSQL (Production)");
    }
    else
    {
        options.UseSqlServer(connectionString);
        Console.WriteLine("✅ Using SQL Server (Local)");
    }
});

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── SMS Service ──
builder.Services.AddHttpClient<SmsService>();

// ── Background Reminder Service ──
builder.Services.AddHostedService<MedicineReminderService>();

var app = builder.Build();

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Medicine Reminder API v1");
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.Run();