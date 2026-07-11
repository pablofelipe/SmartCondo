using SmartCondoApi.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartCondoApi.Dto
{
    public class MessageCreateDto : IValidatableObject
    {
        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Content { get; set; }

        [Required]
        public MessageScope Scope { get; set; }

        public long? RecipientUserId { get; set; }
        public int? CondominiumId { get; set; }
        public int? TowerId { get; set; }
        public int? FloorId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Scope == MessageScope.Individual && !RecipientUserId.HasValue)
            {
                yield return new ValidationResult("RecipientId is required for individual messages", [nameof(RecipientUserId)]);
            }

            // Se for mensagem para grupo e não especificou condomínio
            if (Scope != MessageScope.Individual && !CondominiumId.HasValue)
            {
                yield return new ValidationResult(
                    "CondominiumId is required for group messages",
                    [nameof(CondominiumId)]);
            }

            if ((Scope == MessageScope.Condominium || Scope == MessageScope.Tower || Scope == MessageScope.Floor) && !CondominiumId.HasValue)
            {
                yield return new ValidationResult("CondominiumId is required for group messages", [nameof(CondominiumId)]);
            }

            if ((Scope == MessageScope.Tower || Scope == MessageScope.Floor) && !TowerId.HasValue)
            {
                yield return new ValidationResult("TowerId is required for tower/floor messages", [nameof(TowerId)]);
            }

            if (Scope == MessageScope.Floor && !FloorId.HasValue)
            {
                yield return new ValidationResult("FloorId is required for floor messages", [nameof(FloorId)]);
            }
        }
    }
}
