namespace WebArsip.Mvc.Models.ViewModels
{
    public class AuditLogViewModel
    {
        public int AuditLogId { get; set; }
        public string? UserId { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
    }
}
