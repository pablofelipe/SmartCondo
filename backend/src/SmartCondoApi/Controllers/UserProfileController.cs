using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using System.Security.Claims;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserProfileController(IUserProfileControllerDependencies _dependencies) : ControllerBase
    {
        // Adicionar um usuário
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> AddUser([FromBody] UserProfileCreateDTO userCreateDTO)
        {
            try
            {
                var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var userProfileResponseDTO = await _dependencies.UserProfileService.Add(userCreateDTO, callerRole);

                var _logger = _dependencies.Logger;

                _logger.LogInformation($"userProfile.Id: {userProfileResponseDTO.Id}, token: {userProfileResponseDTO.Token}");

                string confirmationLink = _dependencies.LinkGeneratorService.GenerateConfirmationLink(
                    "ConfirmEmail",
                    "UserProfile",
                    new
                    {
                        userId = userProfileResponseDTO.Id,
                        token = userProfileResponseDTO.Token
                    });

                _logger.LogDebug($"confirmationLink: {confirmationLink}");

                await _dependencies.EmailService.SendEmailAsync(
                    userCreateDTO.User.Email,
                    "Confirme seu e-mail",
                    $"Por favor, confirme seu e-mail clicando neste link: {confirmationLink}");

                return Ok(userProfileResponseDTO);
            }
            catch (InvalidRegistrationNumberIDException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (InvalidCredentialsException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (InconsistentDataException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (LoginAlreadyExistsException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (UserAlreadyExistsException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (CondominiumDisabledException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (UsersExceedException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (ParkingSpaceNumberException ex)
            {
                return Unauthorized(new { ex.Message });
            }
            catch (UnauthorizedAccessException ex)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ocorreu um erro interno. Mensagem: {ex.Message}" });
            }
        }

        // Confirmação do email para finalização do cadastro de usuário
        //[HttpGet("confirm-email")]
        [HttpGet("confirm-email/{userId}/{token}", Name = "ConfirmEmail")]
        public async Task<ActionResult> ConfirmEmail(string userId, string token)
        {
            try
            {
                await _dependencies.EmailConfirmationService.ConfirmEmail(userId, token);

                return Ok("E-mail confirmado com sucesso.");
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (ConfirmEmailException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }

        // Atualizar um usuário
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateUser(long id, [FromBody] UserProfileUpdateDTO updatedUser)
        {
            try
            {
                var userResponseDTO = await _dependencies.UserProfileService.Update(id, updatedUser);
                return Ok(userResponseDTO);
            }
            catch (InvalidCredentialsException ex)
            {
                return BadRequest(new { ex.Message });
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

        ////Obter todos os usuários
        //[HttpGet]
        //[Authorize]
        //public async Task<IEnumerable<UserProfile>> Get()
        //{
        //    return await _dependencies.UserProfileService.Get();
        //}

        // Obter um usuário por ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> GetUser(long id)
        {
            try
            {
                var user = await _dependencies.UserProfileService.Get(id);

                return Ok(user);
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

        //Deletar um usuário por ID
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                await _dependencies.UserProfileService.Delete(id);

                return Ok();
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
    }
}
