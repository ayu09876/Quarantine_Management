using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quarantine_Management.Models
{
    public class DispositionModel
    {
        public string? id { get; set; }
        public string? disposition { get; set; }
        public string? record_date { get; set; }
        public string? modify_by { get; set; }
    }
}
