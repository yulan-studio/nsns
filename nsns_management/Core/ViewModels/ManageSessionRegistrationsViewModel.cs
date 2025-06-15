using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models;
using Core.ViewModels;

namespace Core.ViewModels
{
    public class ManageSessionRegistrationsViewModel
    {
        public int ChildID { get; set; }
        public int CourseID { get; set; }
        public List<SessionOption> AvailableSessions { get; set; } = new();

        public class SessionOption
        {
            public int EnrollmentID { get; set; } // from the session (Status = "Open")
            public DateTime ScheduledAt { get; set; }
            public decimal ScheduledHours { get; set; }
            public bool IsSelected { get; set; }
        }
    }
}