namespace SmartCondoApi.Models.Permissions
{
    public static class UserTypeRoles
    {
        public static readonly string[] SystemAdmins = { "SystemAdministrator" };
        public static readonly string[] CondoAdmins = { "CondominiumAdministrator", "ResidentCommitteeMember" };
        public static readonly string[] Employees = { "Janitor", "Doorman", "Cleaner", "Security", "CleaningManager", "ITSupport" };
        public static readonly string[] ServiceProviders = { "ServiceProvider", "ExternalProvider", "DeliveryPerson", "Supplier" };
        public static readonly string[] Residents = { "Resident" };
        public static readonly string[] ExternalEntities = { "Auditor", "Visitor", "FireDepartment" };

        public static bool IsSystemAdmin(string userType) => SystemAdmins.Contains(userType);
        public static bool IsCondoAdmin(string userType) => CondoAdmins.Contains(userType);
        public static bool IsEmployee(string userType) => Employees.Contains(userType);
        public static bool IsServiceProvider(string userType) => ServiceProviders.Contains(userType);
        public static bool IsResident(string userType) => Residents.Contains(userType);
        public static bool IsExternal(string userType) => ExternalEntities.Contains(userType);
    }
}
