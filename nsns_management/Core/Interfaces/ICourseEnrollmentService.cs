using Core.Models;
using Core.ViewModels;
using Core.Repositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface ICourseEnrollmentService
    {
        Task<CourseEnrollment> GetAsync(int enrollmentId);

        Task<bool> RemoveAsync(int enrollmentId);
        Task<bool> AddRegisteredEnrollmentAsync(int childId, int courseId, decimal scheduledHours, string status, User user);
        Task<bool> RemoveRegisteredEnrollmentAsync(int enrollmentId);

        Task<bool> AddSessionRegisteredEnrollmentAsync(int childId, int courseId, DateTime? scheduledAt, decimal? scheduledHours, int enrollmentId_Ref, string status, User user);
        Task<IEnumerable<CourseEnrollmentViewModel>> GetRegisteredEnrollmentsByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseChildAsync(int courseId, int childId);
        Task<IEnumerable<CourseEnrollment>> GetCompletedEnrollmentsByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollment>> GetScheduledEnrollmentsByCourseAsync(int courseId);

        Task<IEnumerable<CourseEnrollment>> GetRegisteredEnrollmentsByCourseAsync(int courseId);

        Task<IEnumerable<CourseEnrollment>> GetOpenSessionsByCourseAsync(int courseId);

        Task<IEnumerable<CourseEnrollment>> GetClosedSessionsByCourseAsync(int courseId);

        Task<IEnumerable<CourseEnrollment>> GetCanceledSessionsByCourseAsync(int courseId);

        Task<IEnumerable<CourseEnrollment>> GetCompletedSessionsByCourseAsync(int courseId);

        Task<IEnumerable<CourseEnrollment>> GetAllUpcomingSessionsByCourseAsync(int courseId);

        //Task<IEnumerable<Child>> GetRegisteredChildrenByCoachAsync(int coachId);

        //Task<IEnumerable<Core.ViewModels.RegisteredChild>> GetRegisterationByCoachAsync(int coachId);
        Task<IEnumerable<Core.ViewModels.ChildViewModel>> GetRegisterationByCourseAsync(int courseId);

        Task<bool> ScheduleCourseAsync(int childId, int courseId, DateTime scheduledAt, decimal scheduledHours, int coachId, int enrollmentId_Ref);

        Task<bool> AddSessionToGroupCourseAsync(int courseId, DateTime scheduledAt, decimal scheduledHours, string location, string staffNote, User user);

        Task<bool> UpdateSessionAsync(CourseEnrollment session);

        Task<bool> RemoveScheduleAsync(int enrollmentId);

        Task<IEnumerable<CourseEnrollment>> GetSchedulesByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollment>> GetSchedulesByCourseChildAsync(int courseId, int childId);

        Task<IEnumerable<CourseEnrollment>> GetCompletesByCourseChildAsync(int courseId, int childId);

        Task<bool> CompleteCourseAsync(int enrollmentId, Decimal actualHours);
    }


}
