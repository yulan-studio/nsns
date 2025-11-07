using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models;

namespace Core.ViewModels
{
    
    public class HoursViewModel
    {
        public int EnrollmentID { get; set; }
        public string CourseName { get; set; }

        public string ChildName { get; set; }

        public DateTime SessionDate { get; set; }

        public decimal SessionHours { get; set; }
        
        //public decimal IncomeChange { get; set; }
        //public decimal TotalIncomeSoFar { get; set; }
    }

    public class CoachHoursViewModel
    {
        public required Coach Coach { get; set; }
        public List<HoursViewModel>? HoursDetails { get; set; }
    }
}
