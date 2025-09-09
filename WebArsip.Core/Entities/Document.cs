using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.Entities
{
    public class Document
    {
        [Key]
        public int DocId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; }

        // Relasi ke Permission
        public ICollection<Permission>? Permissions { get; set; }

        // Relasi ke Archive
        public Archive? Archive { get; set; }

    }
}
