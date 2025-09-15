namespace WebArsip.Core.DTOs
{
    public class AuditLogRoleStatsDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime Date { get; set; }
    }
}