using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Services.Auth;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController(IAuthService _authService, ILogger<AuthController> _logger) : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] Dictionary<string, string> body)
        {
            try
            {
                var loginResponse = await _authService.Login(body);

                _logger.LogInformation($"Usuário {loginResponse.User.Email} logado");

                return Ok(loginResponse);
            }
            catch (InvalidCredentialsException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (UserTypeNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (UnconfirmedEmailException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (UserLockedException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (UserDisabledException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (UserExpiredException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (IncorrectPasswordException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no login.");
                return StatusCode(500, new { ex.Message });
            }
        }

        [EnableRateLimiting("PublicKeyRateLimit")]
        [HttpGet("public-key")]
        [AllowAnonymous]
        public ActionResult GetPublicKey()
        {
            try
            {
                var result = _authService.GetPublicKey();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar chave pública. Mensagem: {ex.Message}");
                return StatusCode(500, new
                {
                    Message = "Erro interno ao processar chave",
                    Detail = ex.Message
                });
            }
        }
    }
}
