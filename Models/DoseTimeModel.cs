namespace Final_Backend_API.Models
{
    public class DoseTimeModel
    {
        public int Id { get; set; }

        public int MedicineId { get; set; }  // 🔗 link to Medicine

        public TimeSpan DoseTime { get; set; } // "08:00"
    }
}