using System;

namespace WebArsip.Core.Entities
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string UserId { get; set; } = string.Empty; // ID atau Email User
        public string Action { get; set; } = string.Empty; // Create, Update, Delete, etc
        public string EntityName { get; set; } = string.Empty; // Document, User, Role, etc
        public string EntityId { get; set; } = string.Empty; // misalnya DocumentId = 5
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Details { get; set; } = string.Empty; // tambahan info
    }
}