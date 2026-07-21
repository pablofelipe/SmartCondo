using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;

namespace SmartCondoApi.Infra
{
    public static class SmartCondoContextExtensions
    {
        public static Task<int?> GetActorCondominiumIdAsync(this SmartCondoContext context, long actorId)
        {
            return context.UserProfiles
                .Where(u => u.Id == actorId)
                .Select(u => u.CondominiumId)
                .FirstOrDefaultAsync();
        }
    }
}
