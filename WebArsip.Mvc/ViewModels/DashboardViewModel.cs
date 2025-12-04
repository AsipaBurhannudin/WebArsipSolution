namespace WebArsip.Mvc.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int DocumentCount { get; set; }
        public int UserCount { get; set; }
        public int AuditLogCount { get; set; }
        public int RoleCount { get; set; }
        public int PermissionCount { get; set; }
        public int UserPermissionCount {  get; set; }
    }
}