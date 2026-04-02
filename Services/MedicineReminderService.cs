using Final_Backend_API.Data;
using Final_Backend_API.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Final_Backend_API.Services
{
    public class MedicineReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public MedicineReminderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("✅ MedicineReminderService started...");

            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("⏰ Checking reminders at: " + DateTime.Now.ToString("hh:mm tt"));

                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();

                    // ── Get all active medicines ──
                    var medicines = db.Medicines
                        .Where(m => m.IsActive)
                        .ToList();

                    foreach (var med in medicines)
                    {
                        // ── 1. Get user — must have phone number ──
                        var user = db.Users.Find(med.UserId);
                        if (user == null || string.IsNullOrWhiteSpace(user.Phone))
                        {
                            Console.WriteLine($"⚠️ No user/phone for medicine: {med.MedicineName}");
                            continue;
                        }

                        // ── 2. Check IsReminderOn ──
                        var alert = db.AlertSettings
                            .FirstOrDefault(a => a.MedicineId == med.Id);

                        if (alert == null || !alert.IsReminderOn)
                        {
                            Console.WriteLine($"🔕 Reminder OFF: {med.MedicineName}");
                            continue;
                        }

                        // ── 3. Check today is a scheduled day ──
                        var freq = db.FrequencySchedules
                            .FirstOrDefault(f => f.MedicineId == med.Id);

                        if (freq != null && !IsTodayScheduled(freq))
                        {
                            Console.WriteLine($"📅 Not scheduled today: {med.MedicineName}");
                            continue;
                        }

                        // ── 4. Check within StartDate / EndDate ──
                        var schedule = db.MedicineSchedules
                            .FirstOrDefault(s => s.MedicineId == med.Id);

                        if (schedule != null)
                        {
                            var today = DateTime.Today;
                            if (today < schedule.StartDate.Date)
                            {
                                Console.WriteLine($"📅 Not started yet: {med.MedicineName}");
                                continue;
                            }
                            if (schedule.EndDate.HasValue && today > schedule.EndDate.Value.Date)
                            {
                                Console.WriteLine($"📅 Course ended: {med.MedicineName}");
                                continue;
                            }
                        }

                        // ── 5. Already taken today? Skip ──
                        var alreadyTaken = db.MedicineLogs.Any(ml =>
                            ml.MedicineId == med.Id &&
                            ml.LogDate.Date == DateTime.Today &&
                            ml.Status == "taken");

                        if (alreadyTaken)
                        {
                            Console.WriteLine($"✅ Already taken today: {med.MedicineName}");
                            continue;
                        }

                        // ── 6. Match dose time with current time ──
                        var doseTimes = db.DoseTimes
                            .Where(d => d.MedicineId == med.Id)
                            .ToList();

                        var nowTime = DateTime.Now.TimeOfDay;

                        foreach (var doseRow in doseTimes)
                        {
                            var doseTime = doseRow.DoseTime;

                            // Match hour and minute exactly
                            Console.WriteLine($"🕐 Current Time: {DateTime.Now:hh:mm:ss tt}");
                            Console.WriteLine($"💊 Dose Time   : {DateTime.Today.Add(doseTime):hh:mm:ss tt}");

                            if (nowTime.Hours == doseTime.Hours &&
                                nowTime.Minutes == doseTime.Minutes)
                            {
                                Console.WriteLine("✅ TIME MATCHED — SENDING SMS");

                                string formattedTime = DateTime.Today
                                    .Add(doseTime).ToString("hh:mm tt");

                                string message =
                                    $"Reminder: Take {med.MedicineName} " +
                                    $"{med.Dosage}{med.DosageUnit} at {formattedTime}. Stay healthy!";

                                Console.WriteLine($"📲 Sending SMS to {user.Phone} — {med.MedicineName}");

                                await smsService.SendSmsAsync(user.Phone, message);
                            }
                            else
                            {
                                Console.WriteLine("❌ Time not matched");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Reminder error: " + ex.Message);
                }

                // Wait 1 minute then check again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            Console.WriteLine("🛑 MedicineReminderService stopped.");
        }

        // ── Helper: is today a scheduled day? ──
        private bool IsTodayScheduled(FrequencyScheduleDbModel freq)
        {
            return DateTime.Today.DayOfWeek switch
            {
                DayOfWeek.Monday => freq.Monday,
                DayOfWeek.Tuesday => freq.Tuesday,
                DayOfWeek.Wednesday => freq.Wednesday,
                DayOfWeek.Thursday => freq.Thursday,
                DayOfWeek.Friday => freq.Friday,
                DayOfWeek.Saturday => freq.Saturday,
                DayOfWeek.Sunday => freq.Sunday,
                _ => false
            };
        }
    }
}