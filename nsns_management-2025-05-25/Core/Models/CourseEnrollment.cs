
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class CourseEnrollment
    {
        [Key]
        public int EnrollmentID { get; set; }

        [Required]
        public int ChildID { get; set; }
        [ForeignKey("ChildID")]
        public virtual Child Child { get; set; } // Navigation property to Child table (ChildID)

        [Required]
        public int CourseID { get; set; }
        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; } // Navigation property to Course table (CourseID)

        public DateTime? ScheduledAt { get; set; }

        public decimal? ScheduledHours { get; set; }

        public decimal? ActualHours { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        public int? CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public virtual User CreatedByUser { get; set; } // Navigation property to User (CreatedBy)

        public int? UpdatedBy { get; set; }
        [ForeignKey("UpdatedBy")]
        public virtual User UpdatedByUser { get; set; } // Navigation property to User (UpdatedBy)

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
}
