namespace WebArsip.Core.Entities
{
    public class Archive
    {
        public int ArchiveId { get; set; }
        public int DocId { get; set; }
        public DateTime ArchivedAt { get; set; }

        // 🔹 Relasi ke Document
        public Document Document { get; set; }
    }
}
