using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.DTOs
{
    public class SerialNumberGenerateResponseDto
    {
        public bool Success { get; set; }
        public string Generated { get; set; } = string.Empty;
        public long UsedNumber { get; set; }
    }
}
