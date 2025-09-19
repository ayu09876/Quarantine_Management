namespace Quarantine_Management.Models
{
    public class ApproverModel
    {
        public int id { get; set; }
        public string? route_lvl { get; set; }
        public string? route_desc { get; set; }
        public int route_flow { get; set; }
        public string? record_date_up { get; set; }
        public string? record_date{ get; set; }

        public string? usr_sesa { get; set; }
        public string? modify { get; set; }
        public string? usr_name { get; set; }
        public string? modifier_name { get; set; }
        
    }
}
