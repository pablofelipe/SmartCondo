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
                throw new ArgumentException("Usuário não encontrado");

            var userType = await _context.UserTypes.FirstOrDefaultAsync(ut => ut.Id == sender.UserTypeId);

            if (userType == null)
                throw new ArgumentException("Tipo de usuário não encontrado");

            var hasUnrestrictedScope = _permissions.TryGetValue(userType.Name, out var permissions) && permissions.CanManageAllCondominiums;

            if (sender.CondominiumId == null && !hasUnrestrictedScope)
                throw new InvalidOperationException("Usuário não está associado ao condomínio");

            return sender;
        }

        private static int ResolveCondominiumId(MessageCreateDto dto, UserProfile sender)
        {
            // Mensagem individual pode herdar do remetente
            if (dto.Scope == MessageScope.Individual)
                return sender.CondominiumId ?? throw new InvalidOperationException("Sender has no Condominium");

            // Mensagem para grupo requer condomínio explícito ou do remetente
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

            // Verificar se o tipo de usuário do remetente existe nas permissões
            if (!_permissions.TryGetValue(sender.UserType.Name, out var senderPermissions))
            {
                _logger.LogWarning("No permissions configured for user type {UserType}", sender.UserType.Name);
                throw new UnauthorizedAccessException("Your user type is not authorized to send messages");
            }

            // Validar permissões do remetente
            if (!ValidateSenderPermissions(sender, messageDto, senderPermissions))
            {
                _logger.LogWarning("Permission denied for sender {SenderId} to send message of type {Scope}", actor.Id, messageDto.Scope);
                throw new UnauthorizedAccessException("You don't have permission to send this message");
            }

            // Resolve o CondominiumId (DTO ou sender)
            var condominiumId = ResolveCondominiumId(messageDto, sender);

            // Criar a mensagem
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

            // Determinar destinatários
            var recipients = await DetermineRecipients(messageDto, sender, senderPermissions);

            // Criar registros UserMessage para cada destinatário
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
            // Verificar se pode enviar mensagens individuais/grupo
            if (messageDto.Scope == MessageScope.Individual && !senderPermissions.CanSendToIndividuals)
                return false;

            if (messageDto.Scope != MessageScope.Individual && !senderPermissions.CanSendToGroups)
                return false;

            // Verificar destinatário individual
            if (messageDto.Scope == MessageScope.Individual && messageDto.RecipientUserId.HasValue)
            {
                // Verificação adicional será feita em DetermineRecipients
                return true;
            }

            // Verificar se está tentando enviar para outro condomínio
            if (messageDto.CondominiumId.HasValue && messageDto.CondominiumId != sender.CondominiumId)
            {
                // Scope irrestrito é quem pode enviar para outros condomínios
                return senderPermissions.CanManageAllCondominiums;
            }

            return true;
        }

        private async Task<List<UserProfile>> DetermineRecipients(MessageCreateDto messageDto, UserProfile sender, UserPermissionsDTO senderPermissions)
        {
            IQueryable<UserProfile> query = _context.UserProfiles
                .Include(u => u.UserType)
                .AsQueryable();

            // Mensagem individual
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
                    throw new InvalidOperationException("Login de usuario não identificado");

                if (!userRecipient.Enabled)
                    throw new InvalidOperationException("Cannot send message to disabled user");

                // Verificar se o tipo do destinatário é permitido
                if (!senderPermissions.AllowedRecipientTypes.Contains(recipient.UserType.Name))
                {
                    _logger.LogWarning("Sender {SenderType} not allowed to send to recipient {RecipientType}",
                        sender.UserType.Name, recipient.UserType.Name);
                    throw new UnauthorizedAccessException($"You can't send messages to {recipient.UserType.Description}");
                }

                // Verificar se está no mesmo condomínio (exceto para escopo irrestrito)
                if (!senderPermissions.CanManageAllCondominiums &&
                    recipient.CondominiumId != sender.CondominiumId)
                {
                    _logger.LogWarning("Attempt to send message to recipient from different condominium");
                    throw new UnauthorizedAccessException("You can only send messages to users in your condominium");
                }

                return [recipient];
            }

            query = query.Where(me => me.Id != sender.Id);

            // Aplicar filtros baseados no escopo
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
                // Se não especificado, usar o condomínio do remetente
                query = query.Where(u => u.CondominiumId == sender.CondominiumId);
            }

            // Filtrar apenas tipos de destinatários permitidos
            query = query.Where(u => senderPermissions.AllowedRecipientTypes.Contains(u.UserType.Name));

            // Funcionários só podem enviar para residentes (exceto se configurado diferente)
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
                    // Para mensagens enviadas, pegamos o status de leitura do primeiro destinatário
                    IsRead = m.UserMessages.Any() && m.UserMessages.First().IsRead,
                    ReadDate = m.UserMessages.Any() ? m.UserMessages.First().ReadDate : null
                })
                .ToListAsync();

            return messages;
        }

        public async Task<MessageDto> GetMessageAsync(long messageId, AuthenticatedActor actor)
        {
            var userId = actor.Id;

            // Verifica se o usuário tem acesso à mensagem (como remetente ou destinatário)
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
