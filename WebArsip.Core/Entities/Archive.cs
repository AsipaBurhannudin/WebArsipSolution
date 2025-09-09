using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.Entities
{
    public class Archive
    {
        public int ArchiveId { get; set; }
        public int DocId { get; set; }
        public DateTime ArchivedAt { get; set; } = DateTime.Now;

        // Relasi ke Document

        public Document Document { get; set; } = null!;
    }
}
