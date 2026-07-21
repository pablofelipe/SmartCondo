using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Infra;
using SmartCondoApi.Services.Message;
using SmartCondoApi.Services.Notification;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class MessagesController(IMessageService messageService, INotificationService notificationService, ILogger<MessagesController> _logger) : ControllerBase
    {
        private readonly IMessageService _messageService = messageService;
        private readonly INotificationService _notificationService = notificationService;

        [HttpPost]
        public async Task<ActionResult> SendMessage([FromBody] MessageCreateDto messageDto)
        {
            var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);

            try
            {
                var message = await _messageService.SendMessageAsync(messageDto, actor);

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
            var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);

            var messages = await _messageService.GetReceivedMessagesAsync(actor);
            return Ok(messages);
        }

        [HttpGet("sent")]
        public async Task<ActionResult> GetSentMessages()
        {
            var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);

            var messages = await _messageService.GetSentMessagesAsync(actor);
            return Ok(messages);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetMessage(long id)
        {
            var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);

            var message = await _messageService.GetMessageAsync(id, actor);

            if (message == null) return NotFound();

            return Ok(message);
        }

        [HttpPatch("{id}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var actor = AuthenticatedActorFactory.FromClaimsPrincipal(User);

            try
            {
                await _messageService.MarkAsReadAsync(id, actor);

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
