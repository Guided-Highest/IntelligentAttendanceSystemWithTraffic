using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.Services;

namespace IntelligentAttendanceSystem.Interface
{
    public interface IDahuaService_One
    {
        bool Init();
        DahuaService_One Login(string host, int port, string username, string password);
        List<SystemDevice> SearchDevice(int IPCount, List<string> IPList);

    }
}
