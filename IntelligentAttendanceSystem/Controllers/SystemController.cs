using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IntelligentAttendanceSystem.Controllers
{
    public class SystemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDahuaService_One dahuaService;
        public int IPCount { get; set; }
        public List<string> IPList = new List<string>();

        public SystemController(ApplicationDbContext context, IDahuaService_One _dahuaService)
        {
            _context = context;
            this.dahuaService = _dahuaService;
        }

        // GET: System
        public async Task<IActionResult> Index()
        {
            var systemDevices = await _context.SystemDevices
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
            return View(systemDevices);
        }

        // GET: System/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var systemDevice = await _context.SystemDevices
                .FirstOrDefaultAsync(m => m.Id == id);
            if (systemDevice == null)
            {
                return NotFound();
            }

            return View(systemDevice);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchDevices(AddDevice addDevice)
        {
            if (ModelState.IsValid)
            {
                TempData["SuccessMessage"] = "Dahua SDK initialized successfully.";

                if (addDevice.IpRange.IPStart == "" || addDevice.IpRange.IPEnd == "")
                {
                    TempData["ErrorMessage"] = "Please input ip address";
                    return View("Create", addDevice);
                }
                try
                {
                    if (!addDevice.IpRange.IPStart!.Trim().Contains(".") || !addDevice.IpRange.IPEnd!.Trim().Contains("."))
                    {
                        TempData["ErrorMessage"] = "Please input a valid IP address";
                        return View("Create", addDevice);
                    }
                    string[] splitStart = addDevice.IpRange.IPStart!.Trim().Split('.');
                    string[] splitEnd = addDevice.IpRange.IPEnd!.Trim().Split('.');
                    if (splitStart.Length != 4 || splitEnd.Length != 4)
                    {
                        TempData["ErrorMessage"] = "Please input a valid IP address";
                        return View("Create", addDevice);
                    }
                    byte[] startIP = IPAddress.Parse(addDevice.IpRange.IPStart!.Trim()).GetAddressBytes();
                    byte[] endIP = IPAddress.Parse(addDevice.IpRange.IPEnd!.Trim()).GetAddressBytes();
                    if (startIP[0] == endIP[0] && startIP[1] == endIP[1])
                    {
                        if (startIP[2] != endIP[2])
                        {
                            if ((startIP[2] + 1) != endIP[2])
                            {
                                TempData["ErrorMessage"] = "IP amount exceed 256";
                                return View("Create", addDevice);
                            }
                            else
                            {
                                if ((startIP[3] - 1) < endIP[3])
                                {
                                    TempData["ErrorMessage"] = "IP amount exceed 256";
                                    return View("Create", addDevice);
                                }
                                else
                                {
                                    IPCount = 256 + startIP[3] - 1 - endIP[3];
                                    for (int i = startIP[3]; i <= 255; i++)
                                    {
                                        byte[] byIP = new byte[4];
                                        byIP[0] = startIP[0];
                                        byIP[1] = startIP[1];
                                        byIP[2] = startIP[2];
                                        byIP[3] = (byte)i;
                                        IPAddress ip = new IPAddress(byIP);
                                        IPList.Add(ip.ToString());
                                    }
                                    for (int i = 0; i <= endIP[3]; i++)
                                    {
                                        byte[] byIP = new byte[4];
                                        byIP[0] = startIP[0];
                                        byIP[1] = startIP[1];
                                        byIP[2] = endIP[2];
                                        byIP[3] = (byte)i;
                                        IPAddress ip = new IPAddress(byIP);
                                        IPList.Add(ip.ToString());
                                    }
                                }
                            }
                        }
                        else
                        {
                            IPCount = endIP[3] - startIP[3] + 1;
                            for (int i = startIP[3]; i <= endIP[3]; i++)
                            {
                                byte[] byIP = new byte[4];
                                byIP[0] = startIP[0];
                                byIP[1] = startIP[1];
                                byIP[2] = startIP[2];
                                byIP[3] = (byte)i;
                                IPAddress ip = new IPAddress(byIP);
                                IPList.Add(ip.ToString());
                            }
                        }
                        addDevice.systemDevices = dahuaService.SearchDevice(IPCount, IPList);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "IP amount exceed 256";
                        return View("Create", addDevice);
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = "Please input a valid IP address";
                    return View("Create", addDevice);
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to Analyze the IPs.";
            }
            return View("Create", addDevice);
        }
        // POST: System/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitDev(SystemDevice systemDevice)
        {
            if (ModelState.IsValid)
            {
                systemDevice.IsInit = dahuaService.Init();
                if (systemDevice.IsInit)
                {

                    var session = dahuaService.Login(systemDevice.IPAddress, systemDevice.Port, systemDevice.UserName, systemDevice.Password);
                    if (session != null && session.UserId != 0)
                    {
                        var dinfo = session.DeviceInfo;
                        //systemDevice.Status = 
                        //systemDevice.DetailType = session.DetailType;
                        //systemDevice.DeviceType = session.DeviceType;
                        //systemDevice.HttpPort = session.HttpPort;
                    }
                    else
                    {
                        systemDevice.Status = "Connection Failed";
                    }
                    TempData["SuccessMessage"] = "Dahua SDK initialized successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to initialize Dahua SDK.";
                }
            }
            return View("Create", systemDevice);
        }
        // GET: System/Create
        public IActionResult Create()
        {
            return View(new AddDevice());
        }

        // POST: System/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SystemDevice systemDevice)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate IP and Port
                var existingDevice = await _context.SystemDevices
                    .FirstOrDefaultAsync(x => x.IPAddress == systemDevice.IPAddress && x.Port == systemDevice.Port);

                if (existingDevice != null)
                {
                    ModelState.AddModelError("", "A device with this IP address and port already exists.");
                    return View(systemDevice);
                }

                systemDevice.CreatedDate = DateTime.Now;
                _context.Add(systemDevice);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "System device created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(new AddDevice());
        }

        // GET: System/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var systemDevice = await _context.SystemDevices.FindAsync(id);
            if (systemDevice == null)
            {
                return NotFound();
            }
            return View(systemDevice);
        }

        // POST: System/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SystemDevice systemDevice)
        {
            if (id != systemDevice.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate IP and Port (excluding current device)
                    var existingDevice = await _context.SystemDevices
                        .FirstOrDefaultAsync(x => x.IPAddress == systemDevice.IPAddress &&
                                                 x.Port == systemDevice.Port &&
                                                 x.Id != id);

                    if (existingDevice != null)
                    {
                        ModelState.AddModelError("", "A device with this IP address and port already exists.");
                        return View(systemDevice);
                    }

                    var existingSystemDevice = await _context.SystemDevices.FindAsync(id);
                    if (existingSystemDevice == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    existingSystemDevice.Status = systemDevice.Status;
                    existingSystemDevice.IPVersion = systemDevice.IPVersion;
                    existingSystemDevice.IPAddress = systemDevice.IPAddress;
                    existingSystemDevice.Port = systemDevice.Port;
                    existingSystemDevice.SubnetMask = systemDevice.SubnetMask;
                    existingSystemDevice.Gateway = systemDevice.Gateway;
                    existingSystemDevice.MacAddress = systemDevice.MacAddress;
                    existingSystemDevice.DeviceType = systemDevice.DeviceType;
                    existingSystemDevice.DetailType = systemDevice.DetailType;
                    existingSystemDevice.HttpPort = systemDevice.HttpPort;
                    existingSystemDevice.UserName = systemDevice.UserName;

                    // Only update password if provided
                    if (!string.IsNullOrEmpty(systemDevice.Password))
                    {
                        existingSystemDevice.Password = systemDevice.Password;
                    }

                    existingSystemDevice.UpdatedDate = DateTime.Now;

                    _context.Update(existingSystemDevice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "System device updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SystemDeviceExists(systemDevice.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(systemDevice);
        }

        // GET: System/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var systemDevice = await _context.SystemDevices
                .FirstOrDefaultAsync(m => m.Id == id);
            if (systemDevice == null)
            {
                return NotFound();
            }

            return View(systemDevice);
        }

        // POST: System/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var systemDevice = await _context.SystemDevices.FindAsync(id);
            if (systemDevice != null)
            {
                _context.SystemDevices.Remove(systemDevice);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "System device deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SystemDeviceExists(int id)
        {
            return _context.SystemDevices.Any(e => e.Id == id);
        }
    }
}
