using SmartCondoApi.Dto;

namespace SmartCondoApi.Models.Permissions
{
    public static class RolePermissions
    {
        public static Dictionary<string, UserPermissionsDTO> GetPermissions()
        {
            var rawPermissions = RawPermissions;

            foreach (var permission in rawPermissions)
            {
                var userPermission = permission.Value;

                userPermission.CanSendMessages = userPermission.CanSendToIndividuals || userPermission.CanSendToGroups;
                userPermission.CanRegisterVehicles = userPermission.CanRegisterUsers;
                userPermission.CanEditVehicles = userPermission.CanRegisterVehicles;
                userPermission.CanEditUsers = userPermission.CanRegisterUsers;

                userPermission.CanViewVehicles = userPermission.CanViewUsers;
            }

            return rawPermissions;
        }

        private static readonly Dictionary<string, UserPermissionsDTO> RawPermissions = new()
        {
            ["SystemAdministrator"] = new UserPermissionsDTO
            {
                CanManageAllCondominiums = true,
                CanRegisterCondominiums = true,
                CanEditCondominiums = true,
                CanViewCondominiums = true,
                CanRegisterAnyUserType = true,
                CanSendToIndividuals = true,
                CanSendToGroups = true,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = true,
                CanViewUsers = true,
                AllowedRecipientTypes = ["SystemAdministrator", "CondominiumAdministrator", "Resident", "Janitor"],
            },
            ["CondominiumAdministrator"] = new UserPermissionsDTO
            {
                CanRegisterAnyUserType = true,
                CanSendToIndividuals = true,
                CanSendToGroups = true,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = true,
                CanViewUsers = true,
                AllowedRecipientTypes = ["CondominiumAdministrator", "ResidentCommitteeMember", "Resident", "Janitor"],
                BlockedUserTypes = ["SystemAdministrator"]
            },
            ["Resident"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = false,
                CanViewUsers = false,
                IsApartmentOwner = true,
                AllowedRecipientTypes = ["SystemAdministrator", "CondominiumAdministrator"]
            },
            ["Janitor"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = false,
                CanViewUsers = true,
                AllowedRecipientTypes = ["SystemAdministrator", "CondominiumAdministrator", "Resident"]
            },
            ["Doorman"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = true,
                CanViewUsers = true,
                AllowedRecipientTypes = ["SystemAdministrator", "CondominiumAdministrator", "Resident"],
                RegisterableUserTypes = ["Visitor"]
            },
            ["Cleaner"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = false,
                CanViewUsers = false,
                AllowedRecipientTypes = ["CondominiumAdministrator", "Janitor", "CleaningManager"]
            },
            ["Security"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = false,
                CanViewMessages = true,
                CanRegisterUsers = true,
                CanViewUsers = true,
                AllowedRecipientTypes = ["CondominiumAdministrator", "Janitor"],
                RegisterableUserTypes = ["Visitor"]
            },
            ["ServiceProvider"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = false,
                CanViewUsers = true,
                AllowedRecipientTypes = ["CondominiumAdministrator", "Resident"]
            },
            ["ExternalProvider"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = false,
                CanViewMessages = false,
                CanRegisterUsers = false,
                CanViewUsers = false,
                AllowedRecipientTypes = ["Resident"]
            },
            ["DeliveryPerson"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = false,
                CanViewUsers = true,
                AllowedRecipientTypes = ["Resident"]
            },
            ["CleaningManager"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = false,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = true,
                CanViewUsers = false,
                AllowedRecipientTypes = ["CondominiumAdministrator", "Janitor", "Cleaner"],
                RegisterableUserTypes = ["Cleaner"]
            },
            ["Visitor"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = false,
                CanSendToGroups = false,
                CanReceiveMessages = false,
                CanViewMessages = false,
                CanRegisterUsers = false,
                CanViewUsers = false,
            },
            ["ResidentCommitteeMember"] = new UserPermissionsDTO
            {
                CanSendToIndividuals = true,
                CanSendToGroups = true,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = true,
                CanViewUsers = true,
                IsApartmentOwner = true,
                AllowedRecipientTypes = ["CondominiumAdministrator", "ResidentCommitteeMember", "Resident", "Janitor"],
                RegisterableUserTypes = ["Resident"]
            },
            ["AdministrativeAssistant"] = new UserPermissionsDTO
            {
                CanRegisterAnyUserType = true,
                CanSendToIndividuals = true,
                CanSendToGroups = true,
                CanReceiveMessages = true,
                CanViewMessages = true,
                CanRegisterUsers = true,
                CanViewUsers = true,
                AllowedRecipientTypes = ["CondominiumAdministrator", "ResidentCommitteeMember", "Resident", "Janitor"],
                BlockedUserTypes = ["SystemAdministrator", "CondominiumAdministrator"]
            },
        };
    }
}
