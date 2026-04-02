using Final_Backend_API.Data;
using Final_Backend_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql.EntityFrameworkCore.PostgreSQL;
//using Microsoft.OpenApi.Models;  // ✅ ADD THIS

var builder = WebApplication.CreateBuilder(args);

// ── Services ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Medicine Reminder API",
        Version = "v1"
    });
});

// ── Database ──
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Medicine Reminder API v1");
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.Run();