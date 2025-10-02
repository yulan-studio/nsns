using Core.Interfaces;
using Core.Models;
using Core.Repositories;
using Core.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public class ActivityEnrollmentService : IActivityEnrollmentService
    {
        private readonly IActivityEnrollmentRepository _enrollmentRepository;
        private readonly IChildRepository _childRepository;
        private readonly IActivityRepository _activityRepository;

        public ActivityEnrollmentService(IActivityRepository activityRepository, IActivityEnrollmentRepository enrollmentRepository, IChildRepository childRepository)
        {
            _activityRepository = activityRepository;
            _enrollmentRepository = enrollmentRepository;
            _childRepository = childRepository;
        }


        public async Task<bool> IsChildEnrolledInActivity(int userId, int activityId)
        {
            var enrollments = await _enrollmentRepository.GetEnrollmentsByChildAsync(userId, "Registered");
            return enrollments.Any(e => e.ActivityID == activityId);
        }

        public async Task<int> AddRegisteredEnrollmentAsync(int userId, int activityId, string status, User user)
        {
            if (userId <= 0 || activityId <= 0)
                throw new ArgumentException("Invalid child or activity.");

            var child = await _childRepository.GetAsync(userId);

            if (await IsChildEnrolledInActivity(userId, activityId))
                throw new ArgumentException("This activity has already been registered.");

            try
            {
                var enrollment = new ActivityEnrollment
                {
                    //ChildID = childId,
                    ActivityID = activityId,
                    Child = child,
                    Status = status,
                    CreatedBy = user.Id, // Temporary user ID
                    CreatedDate = DateTime.UtcNow
                };

                await _enrollmentRepository.AddAsync(enrollment);

                return enrollment.EnrollmentID;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }

        public async Task<bool> RemoveRegisteredEnrollmentAsync(int enrollmentId)
        {
            //Enrollment removal is only allowed for courses that have not started.
            //var enrollment = await _enrollmentRepository.GetAsync(enrollmentId);
            //var childId = enrollment.ChildID;
            //var courseId = enrollment.CourseID;
            //var course_enrollment = await _enrollmentRepository.GetEnrollmentsByCourseAsync(courseId);
            //if (course_enrollment.Any())
            //{
            //    foreach (var e in course_enrollment)
            //    {
            //        if (e.ChildID == childId && e.Status != "Registered")
            //            throw new Exception("Enrollment removal is only allowed for courses that have not started.");
            //    }

            //}
            return await _enrollmentRepository.RemoveAsync(enrollmentId);
        }


        public async Task<IEnumerable<ActivityEnrollment>> GetAllEnrollmentsByChildAsync(int childId)
        {
            return await _enrollmentRepository.GetAllEnrollmentsByChildAsync(childId);
        }

        public async Task<IEnumerable<ActivityEnrollmentViewModel>> GetUpcomingEnrollmentsViewByChildAsync(int childId)
        {
            return await _enrollmentRepository.GetUpcomingEnrollmentsViewByChildAsync(childId);
        }
        public async Task<IEnumerable<ActivityEnrollment>> GetUpcomingEnrollmentsByChildAsync(int childId)
        {
            return await _enrollmentRepository.GetUpcomingEnrollmentsByChildAsync(childId);
        }

        public async Task<IEnumerable<ActivityEnrollment>> GetRegisteredEnrollmentsByChildAsync(int childId)
        {
            return await _enrollmentRepository.GetEnrollmentsByChildAsync(childId, "Registered");
        }

        public async Task<IEnumerable<ActivityEnrollment>> GetCanceledEnrollmentsByChildAsync(int childId)
        {
            return await _enrollmentRepository.GetEnrollmentsByChildAsync(childId, "Canceled");
        }


        public async Task<IEnumerable<ActivityEnrollment>> GetCompletedEnrollmentsByChildAsync(int childId)
        {
            return await _enrollmentRepository.GetEnrollmentsByChildAsync(childId, "Completed");
        }


        public async Task<IEnumerable<ActivityEnrollment>> UpdateActivityStatusToCompletedAsync()
        {
            return await _enrollmentRepository.UpdateActivityStatusToCompletedAsync();
           
        }

        public async Task<bool> UpdateActivityStatusToCanceledAsync(int activityId)
        {
            return await _enrollmentRepository.UpdateActivityStatusToCanceledAsync(activityId);

        }

        public async Task<bool> UpdateActivityStatusToClosedAsync(int activityId)
        {
            return await _enrollmentRepository.UpdateActivityStatusToClosedAsync(activityId);

        }

        //public async Task<bool> RemoveRegisteredEnrollmentAsync(int enrollmentId)
        //{
        //    //Enrollment removal is only allowed for courses that have not started.
        //    //var enrollment = await _enrollmentRepository.GetAsync(enrollmentId);
        //    //var childId = enrollment.ChildID;
        //    //var courseId = enrollment.CourseID;
        //    //var course_enrollment = await _enrollmentRepository.GetEnrollmentsByCourseAsync(courseId);
        //    //if (course_enrollment.Any())
        //    //{
        //    //    foreach (var e in course_enrollment)
        //    //    {
        //    //        if (e.ChildID == childId && e.Status != "Registered")
        //    //            throw new Exception("Enrollment removal is only allowed for courses that have not started.");
        //    //    }

        //    //}
        //    return await _enrollmentRepository.RemoveAsync(enrollmentId);
        //}

    }
}