namespace Final_Backend_API.Models
{
    public class ReminderToggleModel
    {
        public int Id { get; set; }

        public int MedicineId { get; set; }

        public bool IsReminderOn { get; set; }
    }
}