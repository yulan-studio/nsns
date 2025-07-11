using System;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models;
using Core.ViewModels;

namespace Core.Interfaces
{
    public interface ICourseEnrollmentRepository
    {
        Task<bool> AddAsync(CourseEnrollment enrollment);
        Task<bool> RemoveAsync(int enrollmentId);
        //Task<bool> RemoveRegisteredAsync(int enrollmentId);

        Task<CourseEnrollment> GetAsync(int enrollmentId);

        Task<bool> UpdateAsync(CourseEnrollment entity);
        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByChildAsync(int childId, string status);

        Task<IEnumerable<CourseEnrollmentViewModel>> GetRegisteredEnrollmentsByChildAsync(int childId);  //Get Registered courses for a child, include number of scheduled sessions, number of completed sessions
        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(int courseId);
        //Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCoachAsync(int coachId, string status);
        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseChildAsync(int courseId, int childId, string status);

        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(int courseId, string status);  //yes
    }
}
