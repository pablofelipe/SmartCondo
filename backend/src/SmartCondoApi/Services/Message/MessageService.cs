using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Controllers;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.Message
{
    public class MessageService(SmartCondoContext context, ILogger<MessageService> _logger) : IMessageService
    {
        private readonly SmartCondoContext _context = context;

        private readonly Dictionary<string, UserPermissionsDTO> _permissions = RolePermissions.GetPermissions();

        private async Task<UserProfile> GetSenderWithValidationAsync(long senderId)
        {
            var sender = await _context.UserProfiles
                .Include(u => u.Condominium)
                .FirstOrDefaultAsync(u => u.Id == senderId);

            if (sender == null)
                throw new ArgumentException("User not found");

            var userType = await _context.UserTypes.FirstOrDefaultAsync(ut => ut.Id == sender.UserTypeId);

            if (userType == null)
                throw new ArgumentException("User type not found");

            var hasUnrestrictedScope = _permissions.TryGetValue(userType.Name, out var permissions) && permissions.CanManageAllCondominiums;

            if (sender.CondominiumId == null && !hasUnrestrictedScope)
                throw new InvalidOperationException("User is not associated with a condominium");

            return sender;
        }

        private static int ResolveCondominiumId(MessageCreateDto dto, UserProfile sender)
        {
            // An individual message can inherit the sender's condominium
            if (dto.Scope == MessageScope.Individual)
                return sender.CondominiumId ?? throw new InvalidOperationException("Sender has no Condominium");

            // A group message requires an explicit condominium or the sender's own
            return dto.CondominiumId ?? sender.CondominiumId
                ?? throw new InvalidOperationException("CondominiumId must be specified for group messages");
        }

        public async Task<Models.Message> SendMessageAsync(MessageCreateDto messageDto, AuthenticatedActor actor)
        {
            var sender = await GetSenderWithValidationAsync(actor.Id);

            if (sender == null)
            {
                _logger.LogWarning("Sender not found with ID {SenderId}", actor.Id);
                throw new ArgumentException("Sender not found");
            }

            // Check whether the sender's user type has permissions configured
            if (!_permissions.TryGetValue(sender.UserType.Name, out var senderPermissions))
            {
                _logger.LogWarning("No permissions configured for user type {UserType}", sender.UserType.Name);
                throw new UnauthorizedAccessException("Your user type is not authorized to send messages");
            }

            // Validate the sender's permissions
            if (!ValidateSenderPermissions(sender, messageDto, senderPermissions))
            {
                _logger.LogWarning("Permission denied for sender {SenderId} to send message of type {Scope}", actor.Id, messageDto.Scope);
                throw new UnauthorizedAccessException("You don't have permission to send this message");
            }

            // Resolve the CondominiumId (from the DTO or the sender)
            var condominiumId = ResolveCondominiumId(messageDto, sender);

            // Create the message
            var message = new Models.Message
            {
                Content = messageDto.Content,
                SentDate = DateTime.UtcNow,
                SenderId = actor.Id,
                Scope = messageDto.Scope,
                CondominiumId = condominiumId,
                TowerId = messageDto.TowerId,
                FloorId = messageDto.FloorId,
                RecipientUserId = messageDto.RecipientUserId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Determine the recipients
            var recipients = await DetermineRecipients(messageDto, sender, senderPermissions);

            // Create a UserMessage record for each recipient
            foreach (var recipient in recipients)
            {
                _context.UserMessages.Add(new UserMessage
                {
                    MessageId = message.Id,
                    UserProfileId = recipient.Id,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Message {MessageId} sent by {SenderId} to {RecipientCount} recipients", message.Id, actor.Id, recipients.Count);

            return message;
        }

        private bool ValidateSenderPermissions(UserProfile sender, MessageCreateDto messageDto, UserPermissionsDTO senderPermissions)
        {
            // Check whether individual/group messages are allowed
            if (messageDto.Scope == MessageScope.Individual && !senderPermissions.CanSendToIndividuals)
                return false;

            if (messageDto.Scope != MessageScope.Individual && !senderPermissions.CanSendToGroups)
                return false;

            // Check the individual recipient
            if (messageDto.Scope == MessageScope.Individual && messageDto.RecipientUserId.HasValue)
            {
                // Further validation happens in DetermineRecipients
                return true;
            }

            // Check whether this is an attempt to send to another condominium
            if (messageDto.CondominiumId.HasValue && messageDto.CondominiumId != sender.CondominiumId)
            {
                // Unrestricted scope is what allows sending to other condominiums
                return senderPermissions.CanManageAllCondominiums;
            }

            return true;
        }

        private async Task<List<UserProfile>> DetermineRecipients(MessageCreateDto messageDto, UserProfile sender, UserPermissionsDTO senderPermissions)
        {
            IQueryable<UserProfile> query = _context.UserProfiles
                .Include(u => u.UserType)
                .AsQueryable();

            // Individual message
            if (messageDto.Scope == MessageScope.Individual && messageDto.RecipientUserId.HasValue)
            {
                var recipient = await query.FirstOrDefaultAsync(u => u.Id == messageDto.RecipientUserId.Value);

                if (recipient == null)
                {
                    _logger.LogWarning("Recipient not found with ID {RecipientId}", messageDto.RecipientUserId.Value);
                    return [];
                }

                var userRecipient = await _context.Users.FirstOrDefaultAsync(ur => ur.Id == recipient.Id);

                if (userRecipient == null)
                    throw new InvalidOperationException("Recipient login not found");

                if (!userRecipient.Enabled)
                    throw new InvalidOperationException("Cannot send message to disabled user");

                // Check whether the recipient's user type is allowed
                if (!senderPermissions.AllowedRecipientTypes.Contains(recipient.UserType.Name))
                {
                    _logger.LogWarning("Sender {SenderType} not allowed to send to recipient {RecipientType}",
                        sender.UserType.Name, recipient.UserType.Name);
                    throw new UnauthorizedAccessException($"You can't send messages to {recipient.UserType.Description}");
                }

                // Check whether they're in the same condominium (except for unrestricted scope)
                if (!senderPermissions.CanManageAllCondominiums &&
                    recipient.CondominiumId != sender.CondominiumId)
                {
                    _logger.LogWarning("Attempt to send message to recipient from different condominium");
                    throw new UnauthorizedAccessException("You can only send messages to users in your condominium");
                }

                return [recipient];
            }

            query = query.Where(me => me.Id != sender.Id);

            // Apply filters based on the scope
            if (messageDto.CondominiumId.HasValue)
            {
                query = query.Where(u => u.CondominiumId == messageDto.CondominiumId);

                if (messageDto.Scope >= MessageScope.Tower && messageDto.TowerId.HasValue)
                {
                    query = query.Where(u => u.TowerId == messageDto.TowerId);

                    if (messageDto.Scope >= MessageScope.Floor && messageDto.FloorId.HasValue)
                    {
                        query = query.Where(u => u.FloorNumber == messageDto.FloorId);
                    }
                }
            }
            else if (sender.CondominiumId.HasValue)
            {
                // If not specified, use the sender's own condominium
                query = query.Where(u => u.CondominiumId == sender.CondominiumId);
            }

            // Filter to only the allowed recipient types
            query = query.Where(u => senderPermissions.AllowedRecipientTypes.Contains(u.UserType.Name));

            // Employees may only send to residents (unless configured otherwise)
            if (UserTypeRoles.IsEmployee(sender.UserType.Name) &&
                !senderPermissions.AllowedRecipientTypes.Contains("Resident"))
            {
                query = query.Where(u => UserTypeRoles.IsResident(u.UserType.Name));
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<MessageDto>> GetReceivedMessagesAsync(AuthenticatedActor actor)
        {
            var userId = actor.Id;

            var messages = await _context.UserMessages
                .Include(um => um.Message)
                    .ThenInclude(m => m.Sender)
                        .ThenInclude(s => s.UserType)
                .Include(um => um.Message)
                    .ThenInclude(m => m.Sender)
                        .ThenInclude(s => s.Condominium)
                .Include(um => um.Message)
                    .ThenInclude(m => m.RecipientUser)
                .Include(um => um.Message)
                    .ThenInclude(m => m.Condominium)
                .Include(um => um.Message)
                    .ThenInclude(m => m.Tower)
                .Where(um => um.UserProfileId == userId)
                .OrderByDescending(um => um.Message.SentDate)
                .Select(um => new MessageDto
                {
                    Id = um.Message.Id,
                    Content = um.Message.Content,
                    SentDate = um.Message.SentDate,
                    IsRead = um.IsRead,
                    ReadDate = um.ReadDate,
                    Scope = um.Message.Scope,
                    SenderId = um.Message.SenderId,
                    SenderName = um.Message.Sender.User.UserName,
                    SenderType = um.Message.Sender.UserType.Name,
                    RecipientUserId = um.Message.RecipientUserId,
                    RecipientName = um.Message.RecipientUser != null ? um.Message.RecipientUser.User.UserName : null,
                    CondominiumId = um.Message.CondominiumId,
                    CondominiumName = um.Message.Condominium.Name,
                    TowerId = um.Message.TowerId,
                    TowerName = um.Message.Tower != null ? um.Message.Tower.Name : null,
                    FloorId = um.Message.FloorId
                })
                .ToListAsync();

            return messages;
        }

        public async Task<IEnumerable<MessageDto>> GetSentMessagesAsync(AuthenticatedActor actor)
        {
            var userId = actor.Id;

            var messages = await _context.Messages
                .Include(m => m.Sender)
                    .ThenInclude(s => s.UserType)
                .Include(m => m.Sender)
                    .ThenInclude(s => s.Condominium)
                .Include(m => m.RecipientUser)
                .Include(m => m.Condominium)
                .Include(m => m.Tower)
                .Where(m => m.SenderId == userId)
                .OrderByDescending(m => m.SentDate)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SentDate = m.SentDate,
                    Scope = m.Scope,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.User.UserName,
                    SenderType = m.Sender.UserType.Name,
                    RecipientUserId = m.RecipientUserId,
                    RecipientName = m.RecipientUser != null ? m.RecipientUser.User.UserName : null,
                    CondominiumId = m.CondominiumId,
                    CondominiumName = m.Condominium.Name,
                    TowerId = m.TowerId,
                    TowerName = m.Tower != null ? m.Tower.Name : null,
                    FloorId = m.FloorId,
                    // For sent messages, take the read status from the first recipient
                    IsRead = m.UserMessages.Any() && m.UserMessages.First().IsRead,
                    ReadDate = m.UserMessages.Any() ? m.UserMessages.First().ReadDate : null
                })
                .ToListAsync();

            return messages;
        }

        public async Task<MessageDto> GetMessageAsync(long messageId, AuthenticatedActor actor)
        {
            var userId = actor.Id;

            // Check whether the user has access to the message (as sender or recipient)
            var message = await _context.Messages
                .Include(m => m.Sender)
                    .ThenInclude(s => s.UserType)
                .Include(m => m.Sender)
                    .ThenInclude(s => s.Condominium)
                .Include(m => m.RecipientUser)
                .Include(m => m.Condominium)
                .Include(m => m.Tower)
                .Include(m => m.UserMessages)
                .Where(m => m.Id == messageId &&
                       (m.SenderId == userId || m.UserMessages.Any(um => um.UserProfileId == userId)))
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SentDate = m.SentDate,
                    IsRead = m.UserMessages.Where(um => um.UserProfileId == userId).Select(um => um.IsRead).FirstOrDefault(),
                    ReadDate = m.UserMessages.Where(um => um.UserProfileId == userId).Select(um => um.ReadDate).FirstOrDefault(),
                    Scope = m.Scope,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.User.UserName,
                    SenderType = m.Sender.UserType.Name,
                    RecipientUserId = m.RecipientUserId,
                    RecipientName = m.RecipientUser != null ? m.RecipientUser.User.UserName : null,
                    CondominiumId = m.CondominiumId,
                    CondominiumName = m.Condominium.Name,
                    TowerId = m.TowerId,
                    TowerName = m.Tower != null ? m.Tower.Name : null,
                    FloorId = m.FloorId
                })
                .FirstOrDefaultAsync();

            if (message == null)
            {
                throw new MessageNotFoundException("Message not found or access denied");
            }

            return message;
        }

        public async Task MarkAsReadAsync(long messageId, AuthenticatedActor actor)
        {
            var userMessage = await _context.UserMessages
                .FirstOrDefaultAsync(um => um.MessageId == messageId && um.UserProfileId == actor.Id);

            if (userMessage == null)
            {
                throw new MessageNotFoundException("Message or user not found");
            }

            if (!userMessage.IsRead)
            {
                userMessage.IsRead = true;
                userMessage.ReadDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
