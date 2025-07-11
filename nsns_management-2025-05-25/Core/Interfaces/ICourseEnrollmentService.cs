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

        Task<bool> AddRegisteredEnrollmentAsync(int childId, int courseId, decimal scheduledHours, string status, User user);
        Task<bool> RemoveRegisteredEnrollmentAsync(int enrollmentId);
        Task<IEnumerable<CourseEnrollmentViewModel>> GetRegisteredEnrollmentsByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollment>> GetCompletedEnrollmentsByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollment>> GetScheduledEnrollmentsByCourseAsync(int courseId);

        Task<IEnumerable<CourseEnrollment>> GetRegisteredEnrollmentsByCourseAsync(int courseId);

        //Task<IEnumerable<Child>> GetRegisteredChildrenByCoachAsync(int coachId);

        //Task<IEnumerable<Core.ViewModels.RegisteredChild>> GetRegisterationByCoachAsync(int coachId);
        Task<IEnumerable<Core.ViewModels.ChildViewModel>> GetRegisterationByCourseAsync(int courseId);

        Task<bool> ScheduleCourseAsync(int childId, int courseId, DateTime scheduledAt, decimal scheduledHours, int coachId);

        Task<bool> RemoveScheduleAsync(int enrollmentId);

        Task<IEnumerable<CourseEnrollment>> GetSchedulesByChildAsync(int childId);

        Task<IEnumerable<CourseEnrollment>> GetSchedulesByCourseChildAsync(int courseId, int childId);

        Task<IEnumerable<CourseEnrollment>> GetCompletesByCourseChildAsync(int courseId, int childId);

        Task<bool> CompleteCourseAsync(int enrollmentId, Decimal actualHours);
    }


}
