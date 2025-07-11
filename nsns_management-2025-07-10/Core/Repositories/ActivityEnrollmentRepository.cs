using Core.Interfaces;
using Core.Models;
using Core.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;




namespace Core.Repositories
{
    public class ActivityEnrollmentRepository : IActivityEnrollmentRepository
    {
        private readonly AppDbContext _context;

        public ActivityEnrollmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ActivityEnrollment>> GetEnrollmentsByChildAsync(int childId, string status)
        {
            return await _context.ActivityEnrollments
                .Include(e => e.Activity)
                //.Include(e => e.Child)
                .Where(e => e.ChildID == childId && e.Status == status)
                .OrderBy(e => e.Activity.ScheduledAt)
                .ToListAsync();
        }

        public async Task<bool> AddAsync(ActivityEnrollment enrollment)
        {
            //_context.Attach(enrollment.Child);
            await _context.ActivityEnrollments.AddAsync(enrollment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveAsync(int enrollmentId)
        {
            var enrollment = await _context.ActivityEnrollments.FindAsync(enrollmentId);
            if (enrollment == null) return false;

            _context.ActivityEnrollments.Remove(enrollment);
            return await _context.SaveChangesAsync() > 0;
        }

      

        public async Task<ActivityEnrollment> GetAsync(int enrollmentId)
        {
            return await _context.ActivityEnrollments
                // .Include(e => e.Child)
                .Include(e => e.Activity)
                .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);
        }


        public async Task<IEnumerable<ActivityEnrollment>> UpdateActivityStatusToCompletedAsync()
        {
            var now = DateTime.Now;
            var enrollments = await _context.ActivityEnrollments
                .Include(e => e.Activity)
                .Where(e => ((DateTime)e.Activity.ScheduledAt).AddDays(1)  <= now && e.Status == "Registered")
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                enrollment.Status = "Completed";
            }

            var changes = await _context.SaveChangesAsync();
            return enrollments;
        }


        public async Task<bool> UpdateActivityStatusToCanceledAsync(int activityId)
        {

            var enrollments = await _context.ActivityEnrollments
                .Where(e => e.Status == "Registered" && e.Activity.ActivityID == activityId)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                enrollment.Status = "Canceled";
            }

            var changes = await _context.SaveChangesAsync();
            return changes >= 0; // Returns true even if 0 rows were affected
        }


        public async Task<bool> UpdateActivityStatusToClosedAsync(int activityId)
        {
            var activity = await _context.Activities.FindAsync(activityId);
            if (activity == null)
                return false;
            

            var enrollments = await _context.ActivityEnrollments
                .Where(e => e.Status == "Registered" && e.Activity.ActivityID == activityId)
                .ToListAsync();

            if (enrollments.Count == activity.MaxCapacity)
            {
                activity.Status = "Closed";
            }
            var changes = await _context.SaveChangesAsync();
            return changes >= 0; // Returns true even if 0 rows were affected


        }

        //public async Task<IEnumerable<ActivityEnrollment>> GetEnrollmentsByChildAsync(int childId, string status)
        //{

        //    return await _context.ActivityEnrollments
        //        .Include(e => e.Activity)
        //        //.Include(e => e.Course.Coach)
        //        .Where(e => e.ChildID == childId && e.Status == status)
        //        .ToListAsync();
        //}

        //public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(int courseId)
        //{
        //    return await _context.CourseEnrollments
        //        //.Include(e => e.Child)
        //        .Where(e => e.CourseID == courseId)
        //        .ToListAsync();
        //}
    }
}
