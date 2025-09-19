using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quarantine_Management.Models
{
    public class RequestModel
    {
        public string? count { get; set; }
        public string? roles { get; set; }
        public string? id { get; set; }
        public string? id_req { get; set; }
        public string? name { get; set; }
        public string? sesa_id { get; set; }
        public string? modify_by { get; set; }
        public string? reference { get; set; }
        public string? source_sloc { get; set; }
        public string? dest_sloc { get; set; }
        public string? source_sloc_detail { get; set; }
        public string? dest_sloc_detail { get; set; }
        public string? source_sloc_id { get; set; }
        public string? dest_sloc_id { get; set; }
        public string? quantity { get; set; }
        public string? ppap { get; set; }
        public string? rack { get; set; }
        public string? row { get; set; }
        public string? column { get; set; }
        public string? remark { get; set; }
        public string? source_issue { get; set; }
        public string? issue_category { get; set; }
        public string? issue_detail { get; set; }
        public string? status { get; set; }
        public string? request_date { get; set; }
        public string? requestor { get; set; }
        public string? max_aging { get; set; }
        public string? picture { get; set; }
        public string? last_update { get; set; }
        public string? finish_date { get; set; }
        public string? filename { get; set; }
        public string? req_id { get; set; }
        public string? box_type { get; set; }
        public string? disposition { get; set; }
        public string? pic { get; set; }
        public string? coment { get; set; }
        public string? sap_status { get; set; }
        public string? final_status { get; set; }
        public string? result { get; set; }
        public string? updated_coment { get; set; }
        public string? verify_coment { get; set; }
        public string? reason { get; set; }
        public string? record_date { get; set; }
        public RequestModel? RequestDetails { get; set; }

    }
}
