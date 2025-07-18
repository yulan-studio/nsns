using System;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models;

namespace Core.Interfaces
{
    public interface IActivityEnrollmentRepository
    {
        Task<bool> AddAsync(ActivityEnrollment enrollment);
        Task<bool> RemoveAsync(int enrollmentId);
        //Task<bool> RemoveRegisteredAsync(int enrollmentId);

        Task<IEnumerable<ActivityEnrollment>> GetEnrollmentsByChildAsync(int childId, string status);

        Task<IEnumerable<ActivityEnrollment>> UpdateActivityStatusToCompletedAsync();

        Task<bool> UpdateActivityStatusToCanceledAsync(int activityId);


        Task<bool> UpdateActivityStatusToClosedAsync(int activityId);

        //Task<CourseEnrollment> GetAsync(int enrollmentId);
        //Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByChildAsync(int childId, string status);
        //Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(int courseId);
    }
}
