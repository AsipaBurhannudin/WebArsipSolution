using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.DTOs
{
    public class SerialNumberFormatDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public long CurrentNumber { get; set; }
        public bool IsActive { get; set; }
        public string? Note { get; set; }
    }
}
