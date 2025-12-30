using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArsip.Core.Entities
{
    public class SerialNumberMonthlyCounter
    {
        public int Id { get; set; }
        public string FormatKey { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public long CurrentNumber { get; set; }
    }
}