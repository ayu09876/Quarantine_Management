using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quarantine_Management.Models
{
    public class DateModel
    {
        public string? Month { get; set; }
    }

    public class DateDataModel
    {
        public string? First_Week_Of_Year { get; set; }
        public string? First_Month_Of_Year { get; set; }
        public string? Date_From { get; set; }
        public string? Date_To { get; set; }
        public string? Week_From { get; set; }
        public string? Week_To { get; set; }
        public string? Current_Week { get; set; }

        public string? Month_From { get; set; }
        public string? Month_To { get; set; }
        public string? Last_Month { get; set; }
        public string? Last2_Month { get; set; }
        public string? Current_Month { get; set; }
        public string? Month_Data { get; set; }
        public string? Latest_Cycle { get; set; }
        public string? First_Year { get; set; }
        public string? Current_Year { get; set; }
    }
}
