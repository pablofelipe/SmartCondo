using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Services.Condominium;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class CondominiumController(ICondominiumService _condominiumService) : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<CondominiumResponseDTO>> Get()
        {
            return await _condominiumService.Get();
        }

        // Obter um condominio por ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> Get(int id)
        {
            try
            {
                var condo = await _condominiumService.Get(id);

                return Ok(condo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        [HttpGet("{condominiumId}/users/search")]
        [Authorize]
        public async Task<ActionResult> SearchUsers(
            [FromRoute] int condominiumId,
            [FromQuery] UserProfileSearchDTO searchDto)
        {
            try
            {
                var users = await _condominiumService.SearchUsers(condominiumId, searchDto);

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult> Search([FromQuery] CondominiumSearchDTO searchDto)
        {
            try
            {
                var condos = await _condominiumService.Search(searchDto);
                return Ok(condos);
            }
            catch (InconsistentDataException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Create([FromBody] CondominiumCreateDTO condoDto)
        {
            try
            {
                var condo = await _condominiumService.Create(condoDto);
                return CreatedAtAction(nameof(Get), new { id = condo.Id }, condo);
            }
            catch (InconsistentDataException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> Update(int id, [FromBody] CondominiumUpdateDTO condoDto)
        {
            try
            {
                await _condominiumService.Update(id, condoDto);
                return NoContent();
            }
            catch (CondominiumNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (InconsistentDataException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _condominiumService.Delete(id);
                return NoContent();
            }
            catch (CondominiumNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }
    }
}
