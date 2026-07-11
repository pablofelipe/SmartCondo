using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Services.Vehicle;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class VehiclesController(IVehicleService _vehicleService) : ControllerBase
    {
        /*
        [HttpGet]
        public async Task<ActionResult> GetVehicles(
            [FromQuery] string? licensePlate,
            [FromQuery] string? model,
            [FromQuery] int? apartmentNumber,
            [FromQuery] int? parkingSpaceNumber,
            [FromQuery] string? ownerName,
            [FromQuery] string? cpfCnpj)
        {
            try
            {
                var vehicleFilterInput = new VehicleFilterInput()
                {
                    LicensePlate = licensePlate,
                    Model = model,
                    ApartmentNumber = apartmentNumber,
                    ParkingSpaceNumber = parkingSpaceNumber,
                    OwnerName = ownerName,
                    RegistrationNumber = cpfCnpj
                };

                var vehicles = await _vehicleService.GetFilteredVehiclesAsync(vehicleFilterInput);

                return Ok(vehicles);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }
        */
    }
}
