using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Core.Repositories;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

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

        public async Task<bool> AddRegisteredEnrollmentAsync(int userId, int activityId, string status, User user)
        {
            if (userId <= 0 || activityId <= 0)
                throw new ArgumentException("Invalid child or activity.");

            var child = await _childRepository.GetAsync(userId);

            if (await IsChildEnrolledInActivity(userId, activityId))
                throw new ArgumentException("This activity has already been registered.");

            var enrollment = new ActivityEnrollment
            {
                //ChildID = childId,
                ActivityID = activityId,
                Child = child,
                Status = status,
                CreatedBy = user.Id, // Temporary user ID
                CreatedDate = DateTime.UtcNow
            };

            return await _enrollmentRepository.AddAsync(enrollment);
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