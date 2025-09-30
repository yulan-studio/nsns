using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ViewModels
{
    public class FeeEditViewModel
    {
        public int FeeID { get; set; }

        public int? CourseEnrollmentID { get; set; }

        public int? ActivityEnrollmentID { get; set; }

        [Required]
        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal? TotalCost { get; set; }

        public bool IsPaid { get; set; }
    }
}
