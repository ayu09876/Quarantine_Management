using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quarantine_Management.Models
{
    public class LoginModel
    {
        public string? id { get; set; }
        public string? sesa_id { get; set; }
        public string? name { get; set; }
        public string? password { get; set; }
        public string? level { get; set; }
        public string? department { get; set; }
        public string? email { get; set; }
        public string? rules { get; set; }
        public string? plant { get; set; }
        public string? roles { get; set; }
        public string? record_date { get; set; }
    }
}
