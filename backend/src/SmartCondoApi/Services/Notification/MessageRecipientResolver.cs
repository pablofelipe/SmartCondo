using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.Notification
{
    // Shared by every INotificationService implementation - which users should be notified for a
    // given message depends only on the message's scope, never on how the push itself is
    // delivered (AWS API Gateway vs a native WebSocket).
    public static class MessageRecipientResolver
    {
        public static async Task<List<long>> ResolveUserIdsAsync(SmartCondoContext context, Models.Message message)
        {
            return message.Scope switch
            {
                MessageScope.Condominium => await context.UserProfiles
                    .Where(u => u.CondominiumId == message.CondominiumId)
                    .Select(u => u.Id)
                    .ToListAsync(),

                MessageScope.Tower => await context.UserProfiles
                    .Where(u => u.CondominiumId == message.CondominiumId && u.TowerId == message.TowerId)
                    .Select(u => u.Id)
                    .ToListAsync(),

                MessageScope.Floor => await context.UserProfiles
                    .Where(u => u.CondominiumId == message.CondominiumId &&
                                u.TowerId == message.TowerId &&
                                u.FloorNumber == message.FloorId)
                    .Select(u => u.Id)
                    .ToListAsync(),

                MessageScope.Individual => message.RecipientUserId.HasValue
                    ? [message.RecipientUserId.Value]
                    : [],

                _ => []
            };
        }
    }
}
