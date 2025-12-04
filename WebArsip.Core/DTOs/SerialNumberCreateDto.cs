using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.DTOs
{
    public class SerialNumberCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public long StartNumber { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
    }
}
