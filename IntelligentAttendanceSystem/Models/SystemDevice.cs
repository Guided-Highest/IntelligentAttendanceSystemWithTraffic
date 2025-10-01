using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligentAttendanceSystem.Models
{
    public class AddDevice
    {
        public AddDevice()
        {
            systemDevices = new List<SystemDevice>();
        }
        public IpRange IpRange { get; set; }
        public List<SystemDevice> systemDevices { get; set; }
    }
    public class IpRange
    {
        [Required]
        [Display(Name = "Start IP Address")]
        [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", ErrorMessage = "Invalid IP Address format")]
        public string IPStart { get; set; } = null!;

        [Required]
        [Display(Name = "End IP Address")]
        [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", ErrorMessage = "Invalid IP Address format")]
        public string IPEnd { get; set; } = null!;
    }
    public class SystemDevice
    {
        public int Id { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; }

        [Display(Name = "IP Version")]
        public string? IPVersion { get; set; }

        [Required]
        [Display(Name = "IP Address")]
        [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", ErrorMessage = "Invalid IP Address format")]
        public string IPAddress { get; set; }

        [Required]
        [Display(Name = "Port")]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public ushort Port { get; set; }

        [Display(Name = "Subnet Mask")]
        public string? SubnetMask { get; set; }

        [Display(Name = "Gateway")]
        public string? Gateway { get; set; }

        [Display(Name = "MAC Address")]
        public string? MacAddress { get; set; }

        [Display(Name = "Device Type")]
        public string? DeviceType { get; set; }

        [Display(Name = "Detail Type")]
        public string? DetailType { get; set; }

        [Display(Name = "HTTP Port")]
        [Range(1, 65535, ErrorMessage = "HTTP Port must be between 1 and 65535")]
        public int? HttpPort { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        public bool IsActive { get; set; } = true;

        [NotMapped]
        public bool IsInit { get; set; }

        [NotMapped]
        public string No { get; set; }
    }
}
