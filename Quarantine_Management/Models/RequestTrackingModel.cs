using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quarantine_Management.Models
{
    public class RequestTrackingModel
    {
        public int id_req { get; set; }
        public string? req_id { get; set; }
        public int change_no { get; set; }
        public string? change_type { get; set; }
        public string? change_remark { get; set; }
        public string? change_by { get; set; }
        public string? change_time { get; set; }
    }
}
