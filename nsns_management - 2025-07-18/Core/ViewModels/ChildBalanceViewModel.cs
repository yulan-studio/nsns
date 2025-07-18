using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ViewModels
{
    public class ChildBalanceViewModel
    {
        public DateTime CreatedDate { get; set; }
        public string Type { get; set; } // "Payment", "Course Session", "Activity"
        public string? CourseName { get; set; }
        public string? ActivityName { get; set; }
        public decimal BalanceChange { get; set; }
        public decimal Balance { get; set; }
    }
}
