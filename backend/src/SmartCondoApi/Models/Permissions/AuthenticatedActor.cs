namespace SmartCondoApi.Models.Permissions
{
    public sealed record AuthenticatedActor(long Id, string Role, bool Enabled, int? CondominiumId, bool CondominiumEnabled);
}
