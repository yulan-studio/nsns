using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ViewModels
{
    public class CoachIncomeViewModel
    {
        public int EnrollmentID { get; set; }
        public string CourseName { get; set; }

        public string ChildName { get; set; }

        public DateTime SessionDate { get; set; }

        public decimal SessionHours { get; set; }
        
        public decimal IncomeChange { get; set; }
        public decimal TotalIncomeSoFar { get; set; }
    }
}
