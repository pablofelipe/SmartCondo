using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Infra;
using SmartCondoApi.Services.Condominium;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TowerController(ITowerService _towerService) : ControllerBase
    {
        //// Obter uma torre por ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<TowerResponseDTO>> Get(int id)
        {
            try
            {
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                var tower = await _towerService.Get(id, actor);
                return Ok(tower);
            }
            catch (TowerNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        [HttpGet("byCondominium/{condominiumId}")]
        [Authorize]
        public async Task<ActionResult<List<TowerResponseDTO>>> GetByCondominium(int condominiumId)
        {
            try
            {
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                var towers = await _towerService.GetByCondominium(condominiumId, actor);
                return Ok(towers);
            }
            catch (CondominiumNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TowerResponseDTO>> Create([FromBody] TowerCreateDTO towerDto)
        {
            try
            {
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                var tower = await _towerService.Create(towerDto, actor);
                return CreatedAtAction(nameof(Get), new { id = tower.Id }, tower);
            }
            catch (InconsistentDataException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (CondominiumNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> Update(int id, [FromBody] TowerUpdateDTO towerDto)
        {
            try
            {
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                await _towerService.Update(id, towerDto, actor);
                return NoContent();
            }
            catch (TowerNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (InconsistentDataException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
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
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                await _towerService.Delete(id, actor);
                return NoContent();
            }
            catch (TowerNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (InconsistentDataException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbidden(ex);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        private static ObjectResult Forbidden(UnauthorizedAccessException ex)
        {
            return new ObjectResult(new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
