using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.DTOs
{
    public class SerialNumberGenerateRequestDto
    {
        public string Key { get; set; } = string.Empty; // template key
        public DateTime? Date { get; set; } // optional override date for preview
    }
}
