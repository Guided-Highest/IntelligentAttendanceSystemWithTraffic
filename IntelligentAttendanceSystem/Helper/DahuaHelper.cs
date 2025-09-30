using IntelligentAttendanceSystem.Constant;
using System.Runtime.InteropServices;
using System.Text;
using NetSDKCS;

namespace IntelligentAttendanceSystem.Helper
{
    public static class DahuaHelper
    {

        /// <summary>
        /// Disconnection callback function, which calls back the devices that have been disconnected from the current network. It does not call back the devices that are actively disconnected by calling the Cl IENT Log Out() function of the SDK.
        /// </summary>
        /// <param name="lLoginID">Device user login handle</param>
        /// <param name="pchDVRIP">DVR device IP</param>
        /// <param name="nDVRPort">DVR device connection port</param>
        /// <param name="dwUser">User data</param>
        public delegate void fDisConnect(int lLoginID, StringBuilder pchDVRIP, int nDVRPort, IntPtr dwUser);

        /// <summary>
        /// Initialize SDK
        /// </summary>
        /// <param name="cbDisConnect">
        /// Retrofit rejection function, see commissioned commission<seealso cref="fDisConnect"/>
        /// </param>
        /// <param name="dwUser">User Info</param>
        /// <returns>true: Success; False: Failure</returns>
        [DllImport(DHConsts.LIBRARYNETSDK)]
        public static extern bool CLIENT_Init(fDisConnect cbDisConnect, IntPtr dwUser);


        /// <summary>
        ///Register a user to the device. When the device sets the user to reuse (the device's default user, such as admin, cannot be set to reuse), you can use this account to register with the device multiple times.
        /// </summary>
        /// <param name="pchDVRIP">Device IP</param>
        /// <param name="wDVRPort">Device port</param>
        /// <param name="pchUserName">username</param>
        /// <param name="pchPassword">user password</param>
        /// <param name="lpDeviceInfo">Equipment information, belongs to the output parameter</param>
        /// <param name="error">Return to login error code</param>
        /// <returns>Failure to return 0, successfully return device ID</returns>
        [DllImport(DHConsts.LIBRARYNETSDK)]
        public static extern long CLIENT_Login(string pchDVRIP, ushort wDVRPort, string pchUserName, string pchPassword, ref NET_DEVICEINFO_Ex lpDeviceInfo, ref int error);


        [DllImport(DHConsts.LIBRARYNETSDK)]
        public static extern bool CLIENT_StopSearchDevices(IntPtr lSearchHandle);

        [DllImport(DHConsts.LIBRARYNETSDK)]
        public static extern bool CLIENT_SearchDevicesByIPs(ref NET_DEVICE_IP_SEARCH_INFO pIpSearchInfo, fSearchDevicesCB cbSearchDevices, IntPtr dwUserData, string szLocalIp, uint dwWaitTime);

    }
}
