using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Quarantine_Management.Models
{
    public class StatusModel
    {
        public string? id { get; set; }
        public string? status { get; set; }
        public string? record_date { get; set; }
        public string? modify_by { get; set; }
    }
}
