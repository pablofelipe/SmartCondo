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
    [Authorize]
    public class CondominiumController(ICondominiumService _condominiumService) : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<CondominiumResponseDTO>> Get()
        {
            var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
            return await _condominiumService.Get(actor);
        }

        // Obter um condominio por ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> Get(int id)
        {
            try
            {
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                var condo = await _condominiumService.Get(id, actor);

                return Ok(condo);
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

        [HttpGet("{condominiumId}/users/search")]
        [Authorize]
        public async Task<ActionResult> SearchUsers(
            [FromRoute] int condominiumId,
            [FromQuery] UserProfileSearchDTO searchDto)
        {
            try
            {
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                var users = await _condominiumService.SearchUsers(condominiumId, searchDto, actor);

                return Ok(users);
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

        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult> Search([FromQuery] CondominiumSearchDTO searchDto)
        {
            try
            {
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                var condos = await _condominiumService.Search(searchDto, actor);
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
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                var condo = await _condominiumService.Create(condoDto, actor);
                return CreatedAtAction(nameof(Get), new { id = condo.Id }, condo);
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

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> Update(int id, [FromBody] CondominiumUpdateDTO condoDto)
        {
            try
            {
                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);
                await _condominiumService.Update(id, condoDto, actor);
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
                await _condominiumService.Delete(id, actor);
                return NoContent();
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
