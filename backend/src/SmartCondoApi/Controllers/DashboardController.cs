using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]

    public class DashboardController(SmartCondoContext _context) : Controller
    {
        [HttpGet("stats")]
        public async Task<ActionResult> GetDashboardStats()
        {
            var totalUsers = await _context.Users.Where(e => e.Enabled).CountAsync();
            var totalVehicles = await _context.Vehicles.Where(e => e.Enabled).CountAsync();
            var recentNotifications = await _context.Messages.Where(m => m.SentDate >= DateTime.UtcNow.AddDays(-7)).CountAsync();

            var stats = new
            {
                TotalUsers = totalUsers,
                TotalVehicles = totalVehicles,
                RecentNotifications = recentNotifications
            };

            return Ok(stats);
        }
    }
}
