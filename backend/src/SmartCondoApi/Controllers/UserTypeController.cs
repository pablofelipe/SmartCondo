using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserTypeController(SmartCondoContext _context) : Controller
    {
        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<UserType>> Get()
        {
            return await _context.UserTypes.OrderBy(n => n.Description).ToListAsync();
        }
    }
}
