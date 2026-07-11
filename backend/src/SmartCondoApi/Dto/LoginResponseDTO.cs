namespace SmartCondoApi.Dto
{
    public class LoginResponseDTO
    {
        public string Token { get; set; }
        public UserProfileDTO User { get; set; }
    }

    public class UserProfileDTO
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Role { get; set; } // Ex: "CondominiumAdministrator"

        public int CondominiumId { get; set; }
        public int? TowerId { get; set; }
        public int? FloorId { get; set; }
        public int? Apartment { get; set; }

        public UserPermissionsDTO Permissions { get; set; }
    }

    public class UserPermissionsDTO
    {
        public bool CanManageAllCondominiums { get; set; }
        public bool CanRegisterCondominiums { get; set; }
        public bool CanEditCondominiums { get; set; }
        public bool CanViewCondominiums { get; set; }
        public bool CanRegisterAnyUserType { get; set; } //Pode adicionar todos menos os que estão em BlockedUserTypes
        public bool CanViewMessages { get; set; }
        public bool CanSendMessages { get; set; }
        public bool CanSendToIndividuals { get; set; }
        public bool CanSendToGroups { get; set; }
        public bool CanReceiveMessages { get; set; }
        public bool CanRegisterUsers { get; set; }
        public bool CanEditUsers { get; set; }
        public bool CanViewUsers { get; set; }
        public bool CanRegisterVehicles { get; set; }
        public bool CanEditVehicles { get; set; }
        public bool CanViewVehicles { get; set; }

        public bool IsApartmentOwner { get; set; }

        public List<string> AllowedRecipientTypes { get; set; } // Usuários com permissão de envio de mensagens

        public List<string> BlockedUserTypes { get; set; } // Usuários sem permissão de adição

        public List<string> RegisterableUserTypes { get; set; } // Usuários com permissão de adição
    }
}
