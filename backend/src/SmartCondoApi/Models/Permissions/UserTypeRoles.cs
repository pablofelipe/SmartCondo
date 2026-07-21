namespace SmartCondoApi.Models.Permissions
{
    public static class UserTypeRoles
    {
        public static readonly string[] Employees = { "Janitor", "Doorman", "Cleaner", "Security", "CleaningManager" };
        public static readonly string[] Residents = { "Resident" };

        public static bool IsEmployee(string userType) => Employees.Contains(userType);
        public static bool IsResident(string userType) => Residents.Contains(userType);
    }
}
