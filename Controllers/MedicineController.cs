using Final_Backend_API.Models;
using Final_Backend_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Final_Backend_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicineController : ControllerBase
    {
        private readonly string _connStr;

        public MedicineController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection");
        }

        // ══════════════════════════════════════
        // GET: api/medicine/test
        // ══════════════════════════════════════
        [HttpGet("test")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                return Ok(new { message = "✅ Database connected!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Connection failed!", error = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // POST: api/medicine/add
        // ══════════════════════════════════════
        [HttpPost("add")]
        public async Task<IActionResult> AddMedicine([FromBody] MedicineModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid data." });

            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                // ── STEP 1: Insert disease into UserDiseases, get its Id ──
                int? diseaseId = null;
                if (!string.IsNullOrWhiteSpace(model.DiseaseName))
                {
                    var diseaseCmd = new SqlCommand(@"
                        INSERT INTO UserDiseases (UserId, DiseaseName, IsActive, CreatedAt)
                        VALUES (@UserId, @DiseaseName, 1, GETDATE());
                        SELECT SCOPE_IDENTITY();", conn);

                    diseaseCmd.Parameters.AddWithValue("@UserId", model.UserId);
                    diseaseCmd.Parameters.AddWithValue("@DiseaseName", model.DiseaseName);
                    diseaseId = Convert.ToInt32(await diseaseCmd.ExecuteScalarAsync());
                }

                // ── STEP 2: Insert into Medicines with DiseaseId ──
                var medicineCmd = new SqlCommand(@"
                    INSERT INTO Medicines
                        (UserId, MedicineName, MedicineForm, Dosage,
                         DosageUnit, QuantityPerDose, StockQuantity,
                         PrescribedBy, Notes, DiseaseId)
                    VALUES
                        (@UserId, @MedicineName, @MedicineForm, @Dosage,
                         @DosageUnit, @QuantityPerDose, @StockQuantity,
                         @PrescribedBy, @Notes, @DiseaseId);
                    SELECT SCOPE_IDENTITY();", conn);

                medicineCmd.Parameters.AddWithValue("@UserId", model.UserId);
                medicineCmd.Parameters.AddWithValue("@MedicineName", model.MedicineName);
                medicineCmd.Parameters.AddWithValue("@MedicineForm", model.MedicineForm);
                medicineCmd.Parameters.AddWithValue("@Dosage", model.Dosage);
                medicineCmd.Parameters.AddWithValue("@DosageUnit", model.DosageUnit);
                medicineCmd.Parameters.AddWithValue("@QuantityPerDose", model.QuantityPerDose ?? "");
                medicineCmd.Parameters.AddWithValue("@StockQuantity", model.StockQuantity);
                medicineCmd.Parameters.AddWithValue("@PrescribedBy", model.PrescribedBy ?? "");
                medicineCmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                medicineCmd.Parameters.AddWithValue("@DiseaseId",
                    diseaseId.HasValue ? (object)diseaseId.Value : DBNull.Value);

                int medicineId = Convert.ToInt32(await medicineCmd.ExecuteScalarAsync());

                // ── STEP 3: MedicineSchedule ──
                var scheduleCmd = new SqlCommand(@"
                    INSERT INTO MedicineSchedule (MedicineId, MealTiming, StartDate, EndDate)
                    VALUES (@MedicineId, @MealTiming, @StartDate, @EndDate)", conn);
                scheduleCmd.Parameters.AddWithValue("@MedicineId", medicineId);
                scheduleCmd.Parameters.AddWithValue("@MealTiming", model.Schedule.MealTiming);
                scheduleCmd.Parameters.AddWithValue("@StartDate", model.Schedule.StartDate);
                scheduleCmd.Parameters.AddWithValue("@EndDate",
                    string.IsNullOrEmpty(model.Schedule.EndDate) ? DBNull.Value : model.Schedule.EndDate);
                await scheduleCmd.ExecuteNonQueryAsync();

                // ── STEP 4: FrequencySchedule ──
                var freqCmd = new SqlCommand(@"
                    INSERT INTO FrequencySchedule
                        (MedicineId, FrequencyType, Monday, Tuesday, Wednesday,
                         Thursday, Friday, Saturday, Sunday)
                    VALUES
                        (@MedicineId, @FrequencyType, @Monday, @Tuesday, @Wednesday,
                         @Thursday, @Friday, @Saturday, @Sunday)", conn);
                freqCmd.Parameters.AddWithValue("@MedicineId", medicineId);
                freqCmd.Parameters.AddWithValue("@FrequencyType", model.Frequency.FrequencyType);
                freqCmd.Parameters.AddWithValue("@Monday", model.Frequency.Monday);
                freqCmd.Parameters.AddWithValue("@Tuesday", model.Frequency.Tuesday);
                freqCmd.Parameters.AddWithValue("@Wednesday", model.Frequency.Wednesday);
                freqCmd.Parameters.AddWithValue("@Thursday", model.Frequency.Thursday);
                freqCmd.Parameters.AddWithValue("@Friday", model.Frequency.Friday);
                freqCmd.Parameters.AddWithValue("@Saturday", model.Frequency.Saturday);
                freqCmd.Parameters.AddWithValue("@Sunday", model.Frequency.Sunday);
                await freqCmd.ExecuteNonQueryAsync();

                // ── STEP 5: DoseTimes ──
                foreach (var time in model.DoseTimes)
                {
                    var doseCmd = new SqlCommand(@"
                INSERT INTO DoseTimes (MedicineId, DoseTime)
                VALUES (@MedicineId, @DoseTime)", conn);
                    doseCmd.Parameters.AddWithValue("@MedicineId", medicineId);
                    doseCmd.Parameters.AddWithValue("@DoseTime", time);
                    await doseCmd.ExecuteNonQueryAsync();
                }

                // ── STEP 6: AlertSettings ──
                var alertCmd = new SqlCommand(@"
                    INSERT INTO AlertSettings
                        (MedicineId, DoseReminder, MissedDoseAlert,
                         LowStockWarning, RefillReminder, PriorityLevel, IsReminderOn)
                    VALUES
                        (@MedicineId, @DoseReminder, @MissedDoseAlert,
                         @LowStockWarning, @RefillReminder, @PriorityLevel, 0)", conn);
                alertCmd.Parameters.AddWithValue("@MedicineId", medicineId);
                alertCmd.Parameters.AddWithValue("@DoseReminder", model.Alerts.DoseReminder);
                alertCmd.Parameters.AddWithValue("@MissedDoseAlert", model.Alerts.MissedDoseAlert);
                alertCmd.Parameters.AddWithValue("@LowStockWarning", model.Alerts.LowStockWarning);
                alertCmd.Parameters.AddWithValue("@RefillReminder", model.Alerts.RefillReminder);
                alertCmd.Parameters.AddWithValue("@PriorityLevel", model.Alerts.PriorityLevel);
                await alertCmd.ExecuteNonQueryAsync();

                return Ok(new { message = "✅ Medicine added successfully!", medicineId, diseaseId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // GET: api/medicine/list/{userId}
        // Returns today's medicines with status
        // ══════════════════════════════════════
        [HttpGet("list/{userId}")]
        public async Task<IActionResult> GetMedicines(int userId)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT
                        m.Id,
                        m.MedicineName,
                        m.MedicineForm,
                        m.Dosage,
                        m.DosageUnit,
                        m.StockQuantity,
                        m.PrescribedBy,
                        m.Notes,
                        s.MealTiming,
                        s.StartDate,
                        s.EndDate,
                        f.FrequencyType,
                        a.PriorityLevel,
                        a.IsReminderOn,
                        d.DoseTime,
                        ml.Status AS TodayStatus
                    FROM Medicines m
                    LEFT JOIN MedicineSchedule  s  ON s.MedicineId = m.Id
                    LEFT JOIN FrequencySchedule f  ON f.MedicineId = m.Id
                    LEFT JOIN AlertSettings     a  ON a.MedicineId = m.Id
                    LEFT JOIN DoseTimes         d  ON d.MedicineId = m.Id
                    LEFT JOIN MedicineLogs      ml ON ml.MedicineId = m.Id
                        AND ml.LogDate = CAST(GETDATE() AS DATE)
                    WHERE m.UserId = @UserId
                    AND   m.IsActive = 1", conn);

                cmd.Parameters.AddWithValue("@UserId", userId);

                var reader = await cmd.ExecuteReaderAsync();
                var medicines = new List<object>();

                while (await reader.ReadAsync())
                {
                    medicines.Add(new
                    {
                        id = reader["Id"],
                        medicineName = reader["MedicineName"],
                        medicineForm = reader["MedicineForm"],
                        dosage = reader["Dosage"],
                        dosageUnit = reader["DosageUnit"],
                        stockQuantity = reader["StockQuantity"],
                        prescribedBy = reader["PrescribedBy"],
                        notes = reader["Notes"],
                        mealTiming = reader["MealTiming"],
                        startDate = reader["StartDate"],
                        endDate = reader["EndDate"],
                        frequencyType = reader["FrequencyType"],
                        priorityLevel = reader["PriorityLevel"],
                        isReminderOn = reader["IsReminderOn"] != DBNull.Value && (bool)reader["IsReminderOn"],
                        doseTime = reader["DoseTime"],
                        // null = pending, "taken" or "skipped" from MedicineLogs
                        todayStatus = reader["TodayStatus"] == DBNull.Value ? "pending" : reader["TodayStatus"].ToString()
                    });
                }

                return Ok(medicines);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // POST: api/medicine/take-skip
        // UPSERT — update if exists, insert if not
        // ══════════════════════════════════════
        [HttpPost("take-skip")]
        public async Task<IActionResult> TakeOrSkip([FromBody] TakeSkipModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var today = DateTime.Now.Date;

                // UPSERT: update existing row for today, or insert new one
                var cmd = new SqlCommand(@"
                    IF EXISTS (
                        SELECT 1 FROM MedicineLogs 
                        WHERE MedicineId = @MedicineId AND LogDate = @LogDate
                    )
                        UPDATE MedicineLogs 
                        SET Status = @Status, TakenAt = @TakenAt
                        WHERE MedicineId = @MedicineId AND LogDate = @LogDate
                    ELSE
                        INSERT INTO MedicineLogs (MedicineId, Status, TakenAt, LogDate)
                        VALUES (@MedicineId, @Status, @TakenAt, @LogDate)", conn);

                cmd.Parameters.AddWithValue("@MedicineId", model.MedicineId);
                cmd.Parameters.AddWithValue("@Status", model.Status);
                cmd.Parameters.AddWithValue("@TakenAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@LogDate", today);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "✅ Status saved!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // DELETE: api/medicine/take-skip/{id}
        // Undo — removes today's log
        // ══════════════════════════════════════
        [HttpDelete("take-skip/{id}")]
        public async Task<IActionResult> UndoTakeSkip(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    DELETE FROM MedicineLogs
                    WHERE MedicineId = @MedicineId
                    AND LogDate = CAST(GETDATE() AS DATE)", conn);

                cmd.Parameters.AddWithValue("@MedicineId", id);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "✅ Undo successful!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // GET: api/medicine/test-sms/{phone}
        // Test SMS sending
        // ══════════════════════════════════════
        [HttpGet("test-sms/{phone}")]
        public async Task<IActionResult> TestSms(string phone,
            [FromServices] SmsService smsService)
        {
            try
            {
                await smsService.SendSmsAsync(phone,
                    "Test: Smart Medicine Reminder SMS is working!");

                return Ok(new { message = "✅ SMS sent to " + phone });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ SMS failed!", error = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // PUT: api/medicine/reminder/{medicineId}
        // Toggle bell reminder
        // ══════════════════════════════════════
        [HttpPut("reminder/{medicineId}")]
        public async Task<IActionResult> ToggleReminder(int medicineId, [FromBody] ReminderToggleModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    UPDATE AlertSettings
                    SET IsReminderOn = @IsReminderOn
                    WHERE MedicineId = @MedicineId", conn);

                cmd.Parameters.AddWithValue("@MedicineId", medicineId);
                cmd.Parameters.AddWithValue("@IsReminderOn", model.IsReminderOn);

                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "✅ Reminder updated!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // GET: api/medicine/history/{userId}?date=2026-03-28&filter=all
        // History page — past logs
        // ══════════════════════════════════════
        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetHistory(int userId, [FromQuery] string filter = "all")
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var whereStatus = filter.ToLower() switch
                {
                    "taken" => "AND ml.Status = 'taken'",
                    "skipped" => "AND ml.Status = 'skipped'",
                    _ => ""
                };

                var cmd = new SqlCommand($@"
                    SELECT
                        m.Id,
                        m.MedicineName,
                        m.MedicineForm,
                        m.Dosage,
                        m.DosageUnit,
                        m.StockQuantity,
                        m.PrescribedBy,
                        m.Notes,
                        s.MealTiming,
                        f.FrequencyType,
                        a.PriorityLevel,
                        a.IsReminderOn,
                        d.DoseTime,
                        ml.Status,
                        ml.TakenAt,
                        ml.LogDate
                    FROM MedicineLogs ml
                    INNER JOIN Medicines         m ON m.Id = ml.MedicineId
                    LEFT JOIN  MedicineSchedule  s ON s.MedicineId = m.Id
                    LEFT JOIN  FrequencySchedule f ON f.MedicineId = m.Id
                    LEFT JOIN  AlertSettings     a ON a.MedicineId = m.Id
                    LEFT JOIN  DoseTimes         d ON d.MedicineId = m.Id
                    WHERE m.UserId = @UserId
                    AND   m.IsActive = 1
                    {whereStatus}
                    ORDER BY ml.LogDate DESC, ml.TakenAt DESC", conn);

                cmd.Parameters.AddWithValue("@UserId", userId);

                var reader = await cmd.ExecuteReaderAsync();
                var history = new List<object>();

                while (await reader.ReadAsync())
                {
                    history.Add(new
                    {
                        id = reader["Id"],
                        medicineName = reader["MedicineName"],
                        medicineForm = reader["MedicineForm"],
                        dosage = reader["Dosage"],
                        dosageUnit = reader["DosageUnit"],
                        stockQuantity = reader["StockQuantity"],
                        prescribedBy = reader["PrescribedBy"],
                        notes = reader["Notes"],
                        mealTiming = reader["MealTiming"],
                        frequencyType = reader["FrequencyType"],
                        priorityLevel = reader["PriorityLevel"],
                        isReminderOn = reader["IsReminderOn"] != DBNull.Value && (bool)reader["IsReminderOn"],
                        doseTime = reader["DoseTime"],
                        status = reader["Status"],
                        takenAt = reader["TakenAt"],
                        logDate = reader["LogDate"]
                    });
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // GET: api/medicine/upcoming/{userId}?date=2026-03-28
        // Upcoming — medicines for a specific date
        // ══════════════════════════════════════
        [HttpGet("upcoming/{userId}")]
        public async Task<IActionResult> GetUpcoming(int userId, [FromQuery] string? date = null)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                // Parse target date or use today
                DateTime targetDate = string.IsNullOrEmpty(date)
                    ? DateTime.Today
                    : DateTime.Parse(date);

                string dayName = targetDate.DayOfWeek.ToString(); // "Monday", "Tuesday" etc.

                var cmd = new SqlCommand($@"
                    SELECT
                        m.Id,
                        m.MedicineName,
                        m.MedicineForm,
                        m.Dosage,
                        m.DosageUnit,
                        m.StockQuantity,
                        m.PrescribedBy,
                        m.Notes,
                        s.MealTiming,
                        s.StartDate,
                        s.EndDate,
                        f.FrequencyType,
                        a.PriorityLevel,
                        a.IsReminderOn,
                        d.DoseTime,
                        ml.Status AS DayStatus
                    FROM Medicines m
                    LEFT JOIN MedicineSchedule  s  ON s.MedicineId = m.Id
                    LEFT JOIN FrequencySchedule f  ON f.MedicineId = m.Id
                    LEFT JOIN AlertSettings     a  ON a.MedicineId = m.Id
                    LEFT JOIN DoseTimes         d  ON d.MedicineId = m.Id
                    LEFT JOIN MedicineLogs      ml ON ml.MedicineId = m.Id
                        AND ml.LogDate = @TargetDate
                    WHERE m.UserId = @UserId
                    AND   m.IsActive = 1
                    AND   f.{dayName} = 1
                    AND   s.StartDate <= @TargetDate
                    AND   (s.EndDate IS NULL OR s.EndDate >= @TargetDate)", conn);

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@TargetDate", targetDate.Date);

                var reader = await cmd.ExecuteReaderAsync();
                var medicines = new List<object>();

                while (await reader.ReadAsync())
                {
                    medicines.Add(new
                    {
                        id = reader["Id"],
                        medicineName = reader["MedicineName"],
                        medicineForm = reader["MedicineForm"],
                        dosage = reader["Dosage"],
                        dosageUnit = reader["DosageUnit"],
                        stockQuantity = reader["StockQuantity"],
                        prescribedBy = reader["PrescribedBy"],
                        notes = reader["Notes"],
                        mealTiming = reader["MealTiming"],
                        startDate = reader["StartDate"],
                        endDate = reader["EndDate"],
                        frequencyType = reader["FrequencyType"],
                        priorityLevel = reader["PriorityLevel"],
                        isReminderOn = reader["IsReminderOn"] != DBNull.Value && (bool)reader["IsReminderOn"],
                        doseTime = reader["DoseTime"],
                        dayStatus = reader["DayStatus"] == DBNull.Value ? "pending" : reader["DayStatus"].ToString()
                    });
                }

                return Ok(medicines);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // PUT: api/medicine/update/{id}
        // ══════════════════════════════════════
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateMedicine(int id, [FromBody] MedicineModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid data." });

            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                // ── STEP 1: Insert disease into UserDiseases, get its Id ──
                int? diseaseId = null;
                if (!string.IsNullOrWhiteSpace(model.DiseaseName))
                {
                    var diseaseCmd = new SqlCommand(@"
                INSERT INTO UserDiseases (UserId, DiseaseName, IsActive, CreatedAt)
                VALUES (@UserId, @DiseaseName, 1, GETDATE());
                SELECT SCOPE_IDENTITY();", conn);
                    diseaseCmd.Parameters.AddWithValue("@UserId", model.UserId);
                    diseaseCmd.Parameters.AddWithValue("@DiseaseName", model.DiseaseName);
                    diseaseId = Convert.ToInt32(await diseaseCmd.ExecuteScalarAsync());
                }

                // ── STEP 2: Update Medicines with DiseaseId ──
                var medicineCmd = new SqlCommand(@"
            UPDATE Medicines SET
                MedicineName = @MedicineName, MedicineForm = @MedicineForm,
                Dosage = @Dosage, DosageUnit = @DosageUnit,
                QuantityPerDose = @QuantityPerDose, StockQuantity = @StockQuantity,
                PrescribedBy = @PrescribedBy, Notes = @Notes,
                DiseaseId = @DiseaseId
            WHERE Id = @Id AND UserId = @UserId", conn);

                medicineCmd.Parameters.AddWithValue("@Id", id);
                medicineCmd.Parameters.AddWithValue("@UserId", model.UserId);
                medicineCmd.Parameters.AddWithValue("@MedicineName", model.MedicineName);
                medicineCmd.Parameters.AddWithValue("@MedicineForm", model.MedicineForm);
                medicineCmd.Parameters.AddWithValue("@Dosage", model.Dosage);
                medicineCmd.Parameters.AddWithValue("@DosageUnit", model.DosageUnit);
                medicineCmd.Parameters.AddWithValue("@QuantityPerDose", model.QuantityPerDose ?? "");
                medicineCmd.Parameters.AddWithValue("@StockQuantity", model.StockQuantity);
                medicineCmd.Parameters.AddWithValue("@PrescribedBy", model.PrescribedBy ?? "");
                medicineCmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                medicineCmd.Parameters.AddWithValue("@DiseaseId",
                    diseaseId.HasValue ? (object)diseaseId.Value : DBNull.Value);

                int rows = await medicineCmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "❌ Medicine not found or unauthorized." });

                // ── STEP 3: Update MedicineSchedule ──
                var scheduleCmd = new SqlCommand(@"
            UPDATE MedicineSchedule SET
                MealTiming = @MealTiming, StartDate = @StartDate, EndDate = @EndDate
            WHERE MedicineId = @MedicineId", conn);
                scheduleCmd.Parameters.AddWithValue("@MedicineId", id);
                scheduleCmd.Parameters.AddWithValue("@MealTiming", model.Schedule.MealTiming);
                scheduleCmd.Parameters.AddWithValue("@StartDate", model.Schedule.StartDate);
                scheduleCmd.Parameters.AddWithValue("@EndDate",
                    string.IsNullOrEmpty(model.Schedule.EndDate) ? DBNull.Value : model.Schedule.EndDate);
                await scheduleCmd.ExecuteNonQueryAsync();

                // ── STEP 4: Update FrequencySchedule ──
                var freqCmd = new SqlCommand(@"
            UPDATE FrequencySchedule SET
                FrequencyType = @FrequencyType, Monday = @Monday, Tuesday = @Tuesday,
                Wednesday = @Wednesday, Thursday = @Thursday, Friday = @Friday,
                Saturday = @Saturday, Sunday = @Sunday
            WHERE MedicineId = @MedicineId", conn);
                freqCmd.Parameters.AddWithValue("@MedicineId", id);
                freqCmd.Parameters.AddWithValue("@FrequencyType", model.Frequency.FrequencyType);
                freqCmd.Parameters.AddWithValue("@Monday", model.Frequency.Monday);
                freqCmd.Parameters.AddWithValue("@Tuesday", model.Frequency.Tuesday);
                freqCmd.Parameters.AddWithValue("@Wednesday", model.Frequency.Wednesday);
                freqCmd.Parameters.AddWithValue("@Thursday", model.Frequency.Thursday);
                freqCmd.Parameters.AddWithValue("@Friday", model.Frequency.Friday);
                freqCmd.Parameters.AddWithValue("@Saturday", model.Frequency.Saturday);
                freqCmd.Parameters.AddWithValue("@Sunday", model.Frequency.Sunday);
                await freqCmd.ExecuteNonQueryAsync();

                // ── STEP 5: Delete old DoseTimes and re-insert ──
                var deleteTimesCmd = new SqlCommand(
                    "DELETE FROM DoseTimes WHERE MedicineId = @MedicineId", conn);
                deleteTimesCmd.Parameters.AddWithValue("@MedicineId", id);
                await deleteTimesCmd.ExecuteNonQueryAsync();

                foreach (var time in model.DoseTimes)
                {
                    var doseCmd = new SqlCommand(@"
                INSERT INTO DoseTimes (MedicineId, DoseTime)
                VALUES (@MedicineId, @DoseTime)", conn);
                    doseCmd.Parameters.AddWithValue("@MedicineId", id);
                    doseCmd.Parameters.AddWithValue("@DoseTime", time);
                    await doseCmd.ExecuteNonQueryAsync();
                }

                // ── STEP 6: Update AlertSettings ──
                var alertCmd = new SqlCommand(@"
            UPDATE AlertSettings SET
                DoseReminder = @DoseReminder, MissedDoseAlert = @MissedDoseAlert,
                LowStockWarning = @LowStockWarning, RefillReminder = @RefillReminder,
                PriorityLevel = @PriorityLevel
            WHERE MedicineId = @MedicineId", conn);
                alertCmd.Parameters.AddWithValue("@MedicineId", id);
                alertCmd.Parameters.AddWithValue("@DoseReminder", model.Alerts.DoseReminder);
                alertCmd.Parameters.AddWithValue("@MissedDoseAlert", model.Alerts.MissedDoseAlert);
                alertCmd.Parameters.AddWithValue("@LowStockWarning", model.Alerts.LowStockWarning);
                alertCmd.Parameters.AddWithValue("@RefillReminder", model.Alerts.RefillReminder);
                alertCmd.Parameters.AddWithValue("@PriorityLevel", model.Alerts.PriorityLevel);
                await alertCmd.ExecuteNonQueryAsync();

                return Ok(new { message = "✅ Medicine updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }


        // ══════════════════════════════════════════════════════════
        //  ADDITIONS to MedicineController.cs
        //  Paste these two endpoints into your existing MedicineController
        //  (inside the class, alongside your existing methods)
        // ══════════════════════════════════════════════════════════

        // ══════════════════════════════════════════════════════════
        // GET: api/medicine/user/{userId}
        // My Medicines page — all active medicines with disease name
        // ══════════════════════════════════════════════════════════
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserMedicines(int userId)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
            SELECT
                m.Id,
                m.MedicineName,
                m.MedicineForm,
                m.Dosage,
                m.DosageUnit,
                m.QuantityPerDose,
                m.StockQuantity,
                m.PrescribedBy,
                m.Notes,
                m.DiseaseId,
                ud.DiseaseName,
                s.MealTiming,
                s.StartDate,
                s.EndDate,
                f.FrequencyType,
                a.PriorityLevel,
                a.IsReminderOn,
                d.DoseTime
            FROM Medicines m
            LEFT JOIN MedicineSchedule  s  ON s.MedicineId = m.Id
            LEFT JOIN FrequencySchedule f  ON f.MedicineId = m.Id
            LEFT JOIN AlertSettings     a  ON a.MedicineId = m.Id
            LEFT JOIN DoseTimes         d  ON d.MedicineId = m.Id
            LEFT JOIN UserDiseases      ud ON ud.Id = m.DiseaseId
            WHERE m.UserId = @UserId
            AND   m.IsActive = 1
            ORDER BY m.Id DESC", conn);

                cmd.Parameters.AddWithValue("@UserId", userId);

                var reader = await cmd.ExecuteReaderAsync();
                var medicines = new List<object>();

                while (await reader.ReadAsync())
                {
                    medicines.Add(new
                    {
                        id = reader["Id"],
                        medicineName = reader["MedicineName"],
                        medicineForm = reader["MedicineForm"],
                        dosage = reader["Dosage"],
                        dosageUnit = reader["DosageUnit"],
                        quantityPerDose = reader["QuantityPerDose"],
                        stockQuantity = reader["StockQuantity"],
                        prescribedBy = reader["PrescribedBy"],
                        notes = reader["Notes"],
                        diseaseId = reader["DiseaseId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DiseaseId"]),
                        diseaseName = reader["DiseaseName"] == DBNull.Value ? "" : reader["DiseaseName"].ToString(),
                        mealTiming = reader["MealTiming"],
                        startDate = reader["StartDate"],
                        endDate = reader["EndDate"],
                        frequencyType = reader["FrequencyType"],
                        priorityLevel = reader["PriorityLevel"],
                        isReminderOn = reader["IsReminderOn"] != DBNull.Value && (bool)reader["IsReminderOn"],
                        doseTime = reader["DoseTime"] == DBNull.Value ? null : reader["DoseTime"].ToString()
                    });
                }

                return Ok(medicines);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════
        // UPDATE AddMedicine to also save DiseaseId
        // Replace your existing AddMedicine INSERT with this updated version:
        // ══════════════════════════════════════════════════════════
        // In the INSERT INTO Medicines command, add DiseaseId:
        //
        //   INSERT INTO Medicines
        //     (UserId, MedicineName, MedicineForm, Dosage,
        //      DosageUnit, QuantityPerDose, StockQuantity,
        //      PrescribedBy, Notes, DiseaseId)                  <-- ADD THIS
        //   VALUES
        //     (@UserId, @MedicineName, @MedicineForm, @Dosage,
        //      @DosageUnit, @QuantityPerDose, @StockQuantity,
        //      @PrescribedBy, @Notes, @DiseaseId);              <-- ADD THIS
        //
        // And add this parameter:
        //   medicineCmd.Parameters.AddWithValue(
        //       "@DiseaseId",
        //       model.DiseaseId.HasValue ? (object)model.DiseaseId.Value : DBNull.Value
        //   );
        //
        // Also add to MedicineModel:
        //   public int? DiseaseId { get; set; }



        // ══════════════════════════════════════
        // DELETE: api/medicine/delete/{id}
        // ══════════════════════════════════════
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(
                    "UPDATE Medicines SET IsActive = 0 WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "✅ Medicine deleted!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }
    }
}