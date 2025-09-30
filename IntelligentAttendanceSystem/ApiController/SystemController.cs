using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntelligentAttendanceSystem.ApiController
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemController> _logger;

        public SystemController(ApplicationDbContext context, ILogger<SystemController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/System
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SystemDevice>>> GetSystemDevices()
        {
            try
            {
                return await _context.SystemDevices
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system devices");
                return StatusCode(500, "Error retrieving system devices");
            }
        }

        // GET: api/System/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SystemDevice>> GetSystemDevice(int id)
        {
            try
            {
                var systemDevice = await _context.SystemDevices.FindAsync(id);

                if (systemDevice == null)
                {
                    return NotFound();
                }

                return systemDevice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving system device with ID {id}");
                return StatusCode(500, "Error retrieving system device");
            }
        }

        // POST: api/System
        [HttpPost]
        public async Task<ActionResult<SystemDevice>> PostSystemDevice(SystemDevice systemDevice)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if IP address already exists
                var existingDevice = await _context.SystemDevices
                    .FirstOrDefaultAsync(x => x.IPAddress == systemDevice.IPAddress && x.Port == systemDevice.Port);

                if (existingDevice != null)
                {
                    return Conflict("A device with this IP address and port already exists.");
                }

                systemDevice.CreatedDate = DateTime.Now;
                _context.SystemDevices.Add(systemDevice);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSystemDevice), new { id = systemDevice.Id }, systemDevice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system device");
                return StatusCode(500, "Error creating system device");
            }
        }

        // PUT: api/System/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSystemDevice(int id, SystemDevice systemDevice)
        {
            try
            {
                if (id != systemDevice.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if IP address already exists for other devices
                var existingDevice = await _context.SystemDevices
                    .FirstOrDefaultAsync(x => x.IPAddress == systemDevice.IPAddress &&
                                             x.Port == systemDevice.Port &&
                                             x.Id != id);

                if (existingDevice != null)
                {
                    return Conflict("A device with this IP address and port already exists.");
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
                existingSystemDevice.Password = systemDevice.Password;
                existingSystemDevice.UpdatedDate = DateTime.Now;

                _context.Entry(existingSystemDevice).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!SystemDeviceExists(id))
                {
                    return NotFound();
                }
                _logger.LogError(ex, $"Concurrency error updating system device with ID {id}");
                return StatusCode(500, "Concurrency error updating system device");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating system device with ID {id}");
                return StatusCode(500, "Error updating system device");
            }
        }

        // DELETE: api/System/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSystemDevice(int id)
        {
            try
            {
                var systemDevice = await _context.SystemDevices.FindAsync(id);
                if (systemDevice == null)
                {
                    return NotFound();
                }

                _context.SystemDevices.Remove(systemDevice);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting system device with ID {id}");
                return StatusCode(500, "Error deleting system device");
            }
        }

        private bool SystemDeviceExists(int id)
        {
            return _context.SystemDevices.Any(e => e.Id == id);
        }
    }
}
