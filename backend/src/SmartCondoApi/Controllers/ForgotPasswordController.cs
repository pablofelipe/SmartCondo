using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Dto;
using SmartCondoApi.Services.Email;
using SmartCondoApi.Services.ForgotPassword;
using SmartCondoApi.Services.LinkGenerator;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ForgotPasswordController(IForgotPasswordService _forgotPasswordService, IConfiguration configuration, IEmailService _emailService) : ControllerBase
    {
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                var response = await _forgotPasswordService.SendResetLinkAsync(request);

                var _frontendUrl = configuration["FrontendSettings:BaseUrl"];
                var _resetPasswordPath = configuration["FrontendSettings:ResetPasswordPath"];

                var confirmationLink = $"{_frontendUrl}{_resetPasswordPath}?userId={response.PasswordReset.UserId}&token={Uri.EscapeDataString(response.PasswordReset.Token)}";

                await _emailService.SendEmailAsync(
                    request.Email,
                    "Redefinição de Senha",
                    $"Por favor, clique no link para redefinir sua senha: {confirmationLink}");

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ocorreu um erro interno. Mensagem: {ex.Message}" });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                var response = await _forgotPasswordService.ResetPasswordAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ocorreu um erro interno. Mensagem: {ex.Message}" });
            }
        }
    }
}