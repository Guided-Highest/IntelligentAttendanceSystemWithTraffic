using System.Runtime.InteropServices;

namespace IntelligentAttendanceSystem.Structures
{
    //// Define the traffic junction event structure based on your SDK documentation
    //[StructLayout(LayoutKind.Sequential)]
    //public struct DH_EVENT_TRAFFICJUNCTION
    //{
    //    public uint nAction;                    // Event action (0: pulse, 1: start, 2: stop)
    //    public SYSTEMTIME UTC;                  // Event time
    //    public uint nEventID;                   // Event ID
    //    public TRAFFIC_CAR_INFO stTrafficCar;   // Vehicle information
    //    public uint dwPicDataLen;               // Picture data length
    //    public IntPtr pPicData;                 // Picture data buffer
    //                                            // Add other fields as per your SDK documentation
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //public struct TRAFFIC_CAR_INFO
    //{
    //    public EM_OBJECT_TYPE emObjectType;     // Object type
    //    public EM_VEHICLE_TYPE emVehicleType;   // Vehicle type
    //    public EM_DIRECTION emDirect;           // Movement direction
    //    public float fSpeed;                    // Speed
    //    public RECT rcObject;                   // Object rectangle
    //                                            // Add other vehicle-related fields
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //public struct SYSTEMTIME
    //{
    //    public ushort wYear;
    //    public ushort wMonth;
    //    public ushort wDayOfWeek;
    //    public ushort wDay;
    //    public ushort wHour;
    //    public ushort wMinute;
    //    public ushort wSecond;
    //    public ushort wMilliseconds;
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //public struct RECT
    //{
    //    public int left;
    //    public int top;
    //    public int right;
    //    public int bottom;
    //}

    //// Enums (define based on your SDK)
    //public enum EM_OBJECT_TYPE
    //{
    //    UNKNOWN = 0,
    //    VEHICLE = 1,
    //    // Add other types as needed
    //}

    //public enum EM_VEHICLE_TYPE
    //{
    //    UNKNOWN = 0,
    //    CAR = 1,
    //    TRUCK = 2,
    //    BUS = 3,
    //    MOTORCYCLE = 4,
    //    // Add other vehicle types as needed
    //}

    //public enum EM_DIRECTION
    //{
    //    UNKNOWN = 0,
    //    LEFT = 1,
    //    RIGHT = 2,
    //    UP = 3,
    //    DOWN = 4,
    //    APPROACH = 5,
    //    AWAY = 6
    //}
}
