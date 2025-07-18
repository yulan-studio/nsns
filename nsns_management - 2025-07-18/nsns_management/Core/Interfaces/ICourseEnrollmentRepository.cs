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

        Task<IEnumerable<CourseEnrollment>> GetRootEnrollmentsByChildAsync(int childId, string status);

        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollment>> GetScheduledSessionsToConfirmByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollment>> GetUpcomingEnrollmentsByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollmentViewModel>> GetRegisteredEnrollmentsByChildAsync(int childId);  //Get Registered courses for a child, include number of scheduled sessions, number of completed sessions
        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(int courseId);
        //Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCoachAsync(int coachId, string status);
        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseChildAsync(int courseId, int childId, string status);

        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseChildAsync(int courseId, int childId);

        Task<IEnumerable<CourseEnrollment>> GetUpcomingEnrollmentsByCourseChildAsync(int courseId, int childId);

        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(int courseId, string status);  //yes

        Task<IEnumerable<CourseEnrollment>> GetSessionsByCourseAsync(int courseId, string status);

        Task<IEnumerable<CourseEnrollment>> GetAllUpcomingSessionsByCourseAsync(int courseId);

        Task<List<int?>> GetRegisteredUpcomingSessionsByCourseAsync(int courseId);

        Task UpdateChildCompletedSessionsAsync(int courseId);
        
        Task UpdateCompletedSessionsAsync(int courseId);

        Task UpdateChildCanceledSessionsAsync(int courseId);

        Task<bool> UpdateCompletedCoursesAsync();

        Task<List<int?>> GetChildrenWithRequestToLeaveAsync();

        Task<List<int?>> GetChildrenWithScheduleConcernsAsync();

        Task<List<int?>> GetChildrenWithRequestsOrConcernsAsync();


    }
}
