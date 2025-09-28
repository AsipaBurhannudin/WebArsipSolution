namespace WebArsip.Mvc.Models.ViewModels
{
    public class AuditLogStatsViewModel
    {
        public string UserId { get; set; } = "";
        public string Action { get; set; } = "";
        public int Count { get; set; }
        public DateTime Date { get; set; }
    }
}