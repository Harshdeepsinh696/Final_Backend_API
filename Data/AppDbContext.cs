using Final_Backend_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Final_Backend_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<MedicineDbModel> Medicines { get; set; }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<DoseTimeModel> DoseTimes { get; set; }
        public DbSet<AlertSettingsDbModel> AlertSettings { get; set; }
        public DbSet<MedicineScheduleDbModel> MedicineSchedules { get; set; }
        public DbSet<FrequencyScheduleDbModel> FrequencySchedules { get; set; }
        public DbSet<MedicineLogDbModel> MedicineLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MedicineDbModel>().ToTable("Medicines");
            modelBuilder.Entity<UserModel>().ToTable("Users");
            modelBuilder.Entity<DoseTimeModel>().ToTable("DoseTimes");
            modelBuilder.Entity<AlertSettingsDbModel>().ToTable("AlertSettings");
            modelBuilder.Entity<MedicineScheduleDbModel>().ToTable("MedicineSchedule");
            modelBuilder.Entity<FrequencyScheduleDbModel>().ToTable("FrequencySchedule");
            modelBuilder.Entity<MedicineLogDbModel>().ToTable("MedicineLogs");
        }
    }
}