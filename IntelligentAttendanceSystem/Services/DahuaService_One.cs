using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Helper;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using NetSDKCS;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace IntelligentAttendanceSystem.Services
{
    public class DahuaService_One : IDahuaService_One, IDisposable
    {
        private bool initialized = false;
        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        /// <value>
        /// The user identifier.
        /// </value>
        public long UserId { get; private set; }
        /// <summary>
        /// Gets the Device Info.
        /// </summary>
        /// <value>
        /// The DeviceInfo identifier.
        /// </value>
        public NET_DEVICEINFO_Ex DeviceInfo { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="DahuaApi" /> is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public bool Connected { get; private set; } = true;

        /// <summary>
        /// Gets the host.
        /// </summary>
        /// <value>
        /// The host.
        /// </value>
        public string Host { get; private set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="DahuaApi"/> class.
        /// </summary>

        /// <summary>
        /// Gets all channels.
        /// </summary>
        /// <value>
        /// All channels.
        /// </value>
        public IReadOnlyCollection<IpChannel> AllChannels { get; private set; } = new List<IpChannel>();
        private List<string> m_LocalIPList = new List<string>();
        private List<IntPtr> m_SearchIDList = new List<IntPtr>();
        private int m_DeviceCount_ByIp = 0;
        private List<DEVICE_NET_INFO_EX> m_DeviceList_ByIp = new List<DEVICE_NET_INFO_EX>();
        private List<NET_DEVICE_NET_INFO_EX2> m_DeviceList = new List<NET_DEVICE_NET_INFO_EX2>();
        private int m_DeviceCount = 0;
        private fSearchDevicesCB m_SearchDevicesCB;
        public DahuaService_One() { }

        private DahuaService_One(long userId, string host, NET_DEVICEINFO_Ex deviceInfo)
        {
            UserId = userId;
            Host = host;
            DeviceInfo = deviceInfo;
            RefreshChannelsInfo(deviceInfo);
        }
        /// <summary>
        /// Initialize digital video recorder
        /// </summary>
        public bool Init()
        {
            if (initialized == false)
            {
                initialized = DahuaHelper.CLIENT_Init(null, IntPtr.Zero);
            }
            return initialized;
        }
        public void Dispose()
        {
        }

        public DahuaService_One Login(string host, int port, string username, string password)
        {
            var deviceInfo = new NET_DEVICEINFO_Ex();
            int error = 0;
            long loginId = DahuaHelper.CLIENT_Login(host, (ushort)port, username, password, ref deviceInfo, ref error);

            return new DahuaService_One(loginId, host, deviceInfo);
        }
        private void RefreshChannelsInfo(NET_DEVICEINFO_Ex deviceInfo)
        {
            List<IpChannel> list = new List<IpChannel>();
            for (var i = 1; i <= deviceInfo.nChanNum; i++)
            {
                list.Add(new IpChannel(i));
            }
            AllChannels = list;
        }
        private void GetAllNetworkInterface()
        {
            m_LocalIPList.Clear();
            string temp_ip = "";
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                if (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    IPInterfaceProperties ip = adapter.GetIPProperties();
                    UnicastIPAddressInformationCollection ipCollection = ip.UnicastAddresses;
                    foreach (UnicastIPAddressInformation ipadd in ipCollection)
                    {
                        if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            temp_ip = ipadd.Address.ToString();
                            if (!m_LocalIPList.Contains(temp_ip))
                            {
                                m_LocalIPList.Add(temp_ip);
                            }
                        }

                    }
                }
            }
        }
        private void StopAllSearchDevice()
        {
            try
            {
                for (int i = 0; i < m_SearchIDList.Count; i++)
                {
                    if (IntPtr.Zero != m_SearchIDList[i])
                    {
                        DahuaHelper.CLIENT_StopSearchDevices(m_SearchIDList[i]);
                        m_SearchIDList[i] = IntPtr.Zero;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

        }
        List<SystemDevice> _systemDevices = new List<SystemDevice>();
        private void UpdateSearchUI(DEVICE_NET_INFO_EX info)
        {
            int index = m_DeviceList_ByIp.FindIndex(p => p.szMac == info.szMac);
            if (-1 == index)
            {
                m_DeviceCount_ByIp++;
                m_DeviceList_ByIp.Add(info);
                var viewItem = new SystemDevice();
                viewItem.No = m_DeviceCount_ByIp.ToString();
                if ((info.byInitStatus & 0x1) == 1)
                {
                    viewItem.IsInit = false;
                    viewItem.Status = "Uninitialized";
                }
                else
                {
                    viewItem.IsInit = true;
                    viewItem.Status = "Initialized";
                }
                viewItem.IPVersion = info.iIPVersion.ToString();
                viewItem.IPAddress = info.szIP;
                viewItem.Port = (ushort)info.nPort;
                viewItem.SubnetMask = info.szSubmask;
                viewItem.Gateway = info.szGateway;
                viewItem.MacAddress = info.szMac;
                viewItem.DeviceType = info.szDeviceType;
                viewItem.DetailType = info.szNewDetailType;
                viewItem.HttpPort = info.nHttpPort;
                _systemDevices.Add(viewItem);
            }
        }
        private void SearchDevicesCB(IntPtr pDevNetInfo, IntPtr pUserData)
        {
            DEVICE_NET_INFO_EX info = (DEVICE_NET_INFO_EX)Marshal.PtrToStructure(pDevNetInfo, typeof(DEVICE_NET_INFO_EX));
            UpdateSearchUI(info);
        }
        public List<SystemDevice> SearchDevice(int IPCount, List<string> IPList)
        {
            GetAllNetworkInterface();
            StopAllSearchDevice();

            m_DeviceList_ByIp.Clear();
            m_DeviceCount_ByIp = 0;


            NET_DEVICE_IP_SEARCH_INFO info = new NET_DEVICE_IP_SEARCH_INFO();
            info.dwSize = (uint)Marshal.SizeOf(typeof(NET_DEVICE_IP_SEARCH_INFO));
            info.nIpNum = IPCount;
            info.szIPs = new NET_IPADDRESS[256];
            for (int i = 0; i < IPCount; i++)
            {
                info.szIPs[i].szIP = IPList[i];
            }
            m_SearchDevicesCB = new fSearchDevicesCB(SearchDevicesCB);
            foreach (var item in m_LocalIPList)
            {
                bool res = DahuaHelper.CLIENT_SearchDevicesByIPs(ref info, m_SearchDevicesCB, IntPtr.Zero, item, 10000);
                if (!res)
                {
                    continue;
                }
            }
            return _systemDevices;
        }
    }
}
