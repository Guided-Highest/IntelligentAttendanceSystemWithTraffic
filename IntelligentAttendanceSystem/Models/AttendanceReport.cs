namespace IntelligentAttendanceSystem.Models
{
    public class AttendanceReport
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Identifier { get; set; } // RollNumber or EmployeeId
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public decimal AttendancePercentage { get; set; }
    }
}