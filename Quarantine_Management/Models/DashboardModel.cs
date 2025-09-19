using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quarantine_Management.Models
{
    public class DashboardModel
    {
        // Original properties
        public int total_request { get; set; }
        public string? part_number { get; set; }
        public int back_to_production { get; set; }
        public int scrap { get; set; }
        public int send_to_blp { get; set; }
        public int send_to_supplier { get; set; }
        public string? reference { get; set; }
        public string? source_issue { get; set; }
        public string? requestor { get; set; }
        public int total { get; set; }
    }
}
