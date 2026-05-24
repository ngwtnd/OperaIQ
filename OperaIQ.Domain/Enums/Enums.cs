namespace OperaIQ.Domain.Enums
{
    public enum TenantStatus { Active, Suspended, Expired }
    public enum ProjectStatus { Active, Completed, Archived }
    public enum TaskStatus { Todo, InProgress, Review, Done, Cancelled }
    public enum TaskPriority { Low, Medium, High, Critical }
    public enum PermissionLevel { View, Download, Edit }
    public enum UserSystemRole { SuperAdmin, TenantOwner, TenantAdmin, Employee }
}
