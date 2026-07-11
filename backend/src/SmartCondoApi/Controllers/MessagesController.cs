using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Message;
using SmartCondoApi.Services.Notification;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class MessagesController(IMessageService messageService, INotificationService notificationService, UserManager<User> userManager, SmartCondoContext context, ILogger<MessagesController> _logger) : ControllerBase
    {
        private readonly IMessageService _messageService = messageService;
        private readonly INotificationService _notificationService = notificationService;
        private readonly UserManager<User> _userManager = userManager;
        private readonly SmartCondoContext _context = context;

        [HttpPost]
        public async Task<ActionResult> SendMessage([FromBody] MessageCreateDto messageDto)
        {
            var userId = _userManager.GetUserId(User);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Id == Convert.ToInt64(userId));

            if (userProfile == null)
            {
                return Unauthorized();
            }

            try
            {
                var message = await _messageService.SendMessageAsync(messageDto, userProfile.Id);

                await _notificationService.NotifyNewMessageAsync(message);

                return Ok(message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ObjectResult(new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = ex.Message,
                    Status = StatusCodes.Status403Forbidden
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ocorreu um erro interno. Mensagem: {ex.Message}" });
            }
        }

        [HttpGet("received")]
        public async Task<ActionResult> GetReceivedMessages()
        {
            var userId = _userManager.GetUserId(User);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Id == Convert.ToInt64(userId));

            if (userProfile == null) return Unauthorized();

            var messages = await _messageService.GetReceivedMessagesAsync(userProfile.Id);
            return Ok(messages);
        }

        [HttpGet("sent")]
        public async Task<ActionResult> GetSentMessages()
        {
            var userId = _userManager.GetUserId(User);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Id == Convert.ToInt64(userId));

            if (userProfile == null) return Unauthorized();

            var messages = await _messageService.GetSentMessagesAsync(userProfile.Id);
            return Ok(messages);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetMessage(long id)
        {
            var userId = _userManager.GetUserId(User);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Id == Convert.ToInt64(userId));

            if (userProfile == null) return Unauthorized();

            var message = await _messageService.GetMessageAsync(id, userProfile.Id);

            if (message == null) return NotFound();

            return Ok(message);
        }

        [HttpPatch("{id}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var userProfileId = Convert.ToInt64(userId);

                await _messageService.MarkAsReadAsync(id, userProfileId);

                return NoContent();
            }
            catch (MessageNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
