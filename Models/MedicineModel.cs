namespace Final_Backend_API.Models
{
    // ══════════════════════════════════════
    // For EF / Database (flat model with Id)
    // ══════════════════════════════════════
    public class MedicineDbModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string MedicineForm { get; set; } = string.Empty;
        public decimal Dosage { get; set; }
        public string DosageUnit { get; set; } = string.Empty;
        public string QuantityPerDose { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string PrescribedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public int? DiseaseId { get; set; }
        public bool IsActive { get; set; } = true;
        // ✅ DiseaseName REMOVED — not a column in your Medicines table
    }

    // ══════════════════════════════════════
    // For Controller (receives full data from frontend)
    // ══════════════════════════════════════
    public class MedicineModel
    {
        public int UserId { get; set; }

        // Medicine name
        // Example → "Paracetamol"
        public string MedicineName { get; set; } = string.Empty;

        // What form is medicine
        // Example → "Tablet" / "Capsule" / "Syrup"
        public string MedicineForm { get; set; } = string.Empty;

        // How much dose
        // Example → 500
        public decimal Dosage { get; set; }

        // Unit of dosage
        // Example → "mg" / "ml"
        public string DosageUnit { get; set; } = string.Empty;

        // How many per dose
        // Example → "1 tablet" / "2 capsules"
        public string QuantityPerDose { get; set; } = string.Empty;

        // How many pills in stock
        // Example → 50
        public int StockQuantity { get; set; }

        // Doctor name
        // Example → "Dr. Sharma"
        public string PrescribedBy { get; set; } = string.Empty;

        // Special instructions
        // Example → "Take with warm water"
        public string Notes { get; set; } = string.Empty;

        public string? DiseaseName { get; set; }
        public int? DiseaseId { get; set; }

        // ── Related Tables ──
        public MedicineScheduleModel Schedule { get; set; }
        public FrequencyScheduleModel Frequency { get; set; }
        public List<string> DoseTimes { get; set; }
        public AlertSettingsModel Alerts { get; set; }
    }

    public class AlertSettingsDbModel
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public bool IsReminderOn { get; set; }
    }

    // ══════════════════════════════════════
    // TABLE 2 — MedicineSchedule
    // ══════════════════════════════════════
    public class MedicineScheduleModel
    {
        // When to take medicine
        // Example → "Before Meal" / "After Meal"
        //           "With Meal" / "Empty Stomach"
        //           "No Restriction"
        public string MealTiming { get; set; } = string.Empty;

        // When to start medicine
        // Example → "2026-03-18"
        public string StartDate { get; set; } = string.Empty;

        // When to stop medicine
        // Example → "2026-04-18" or "" if ongoing
        public string EndDate { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════
    // TABLE 3 — FrequencySchedule
    // ══════════════════════════════════════
    public class FrequencyScheduleModel
    {
        // How often to take medicine
        // Example → "Daily" / "Weekly" / "Custom"
        public string FrequencyType { get; set; } = string.Empty;

        // Which days to take medicine
        // true  = take medicine this day ✅
        // false = skip this day ❌
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
    }

    // ══════════════════════════════════════
    // TABLE 4 — DoseTimes
    // Stored as List<string> in MedicineModel
    // Example → ["08:00", "14:00", "20:00"]
    // ══════════════════════════════════════


    // ══════════════════════════════════════
    // TABLE 5 — AlertSettings
    // ══════════════════════════════════════
    public class AlertSettingsModel
    {
        // Send reminder at dose time?
        public bool DoseReminder { get; set; }

        // Alert if dose missed?
        public bool MissedDoseAlert { get; set; }

        // Warn when pills running low?
        public bool LowStockWarning { get; set; }

        // Remind to buy more pills?
        public bool RefillReminder { get; set; }

        // How important is this medicine
        // Example → "Critical" / "High" / "Medium" / "Low"
        public string PriorityLevel { get; set; } = string.Empty;
    }

    public class MedicineScheduleDbModel
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MealTiming { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    // ── DB model for FrequencySchedule table ──
    public class FrequencyScheduleDbModel
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string FrequencyType { get; set; } = string.Empty;
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
    }

    // ── DB model for MedicineLogs table ──
    public class MedicineLogDbModel
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string Status { get; set; } = string.Empty; // "taken" / "skipped"
        public DateTime TakenAt { get; set; }
        public DateTime LogDate { get; set; }
    }
}