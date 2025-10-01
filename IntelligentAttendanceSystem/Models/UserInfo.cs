namespace IntelligentAttendanceSystem.Models
{
    public class UserInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int UserLevel { get; set; }
        public string CardNumber { get; set; }
        public DateTime RegistrationTime { get; set; }
        public DateTime LastUseTime { get; set; }
        public int UserType { get; set; } // 0=Normal, 1=Blocklist, 2=Guest, etc.
        public string Department { get; set; }
        public bool IsActive { get; set; }
        public string Password { get; set; } // If needed for user management
    }

    public class UserListResponse
    {
        public bool Success { get; set; }
        public List<UserInfo> Users { get; set; }
        public int TotalCount { get; set; }
        public string ErrorMessage { get; set; }
    }
}
