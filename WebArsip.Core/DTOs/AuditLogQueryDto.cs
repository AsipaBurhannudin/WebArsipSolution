namespace WebArsip.Core.DTOs
{
    namespace WebArsip.Core.DTOs
    {
        public class AuditLogQueryDto : BaseQueryDto
        {
            public string? UserId { get; set; }
            public string? Action { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
        }
    }
}