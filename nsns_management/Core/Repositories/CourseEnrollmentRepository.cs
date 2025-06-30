using Core.Interfaces;
using Core.Models;
using Core.ViewModels;
using Core.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;




namespace Core.Repositories
{
    public class CourseEnrollmentRepository : ICourseEnrollmentRepository
    {
        private readonly AppDbContext _context;

        public CourseEnrollmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddAsync(CourseEnrollment enrollment)
        {
            //_context.Attach(enrollment.Child);
            await _context.CourseEnrollments.AddAsync(enrollment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveAsync(int enrollmentId)
        {
            var enrollment = await _context.CourseEnrollments.FindAsync(enrollmentId);
            if (enrollment == null) return false;

            _context.CourseEnrollments.Remove(enrollment);
            return await _context.SaveChangesAsync() > 0;
        }

      

        public async Task<CourseEnrollment> GetAsync(int enrollmentId)
        {
            return await _context.CourseEnrollments
               // .Include(e => e.Child)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);
        }

        public async Task<bool> UpdateAsync(CourseEnrollment entity)
        {
            try
            {
                _context.CourseEnrollments.Update(entity);
                await _context.SaveChangesAsync();  // Commit the changes asynchronously
                return true;
            }
            catch (Exception ex)
            {
                return false; // Return failure in case of an exception
            }
        }

        //Get Registered/Scheduled/completed course session
        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByChildAsync(int childId, string status)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Course)
                .Include(e => e.Course.Coach)
                .Include(e => e.Course.Specialty)
                .Where(e => e.ChildID != null &&e.ChildID == childId && e.Status == status && e.EnrollmentID_Ref != null)
                .OrderBy(e => e.CourseID)
                .OrderBy(e => e.ScheduledAt)
                .ToListAsync();
        }


        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByChildAsync(int childId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Course)
                .Include(e => e.Course.Coach)
                .Include(e => e.Course.Specialty)
                .Where(e => e.ChildID != null && e.ChildID == childId &&  e.EnrollmentID_Ref != null)
                .OrderBy(e => e.CourseID)
                .OrderBy(e => e.ScheduledAt)
                .ToListAsync();
        }


        //Get registered and completed course information
        public async Task<IEnumerable<CourseEnrollmentViewModel>> GetRegisteredEnrollmentsByChildAsync(int childId)  //Get Registered courses for a child, include number of scheduled sessions, number of completed sessions
        {
            return await _context.CourseEnrollments
           .Include(e => e.Course)
           .Include(e => e.Course.Coach)
           .Include(e => e.Course.Specialty)
           .Where(e => e.ChildID == childId && ((e.Status == "Registered" || e.Status == "Completed" ) && e.EnrollmentID_Ref == null))  //Not included those registered to session
           .OrderBy(e => e.CourseID)
           .Select(e => new CourseEnrollmentViewModel
           {
               
               CourseID = e.CourseID,
               CourseType = e.Course.CourseType,
               IsActive = e.Course.IsActive,
               SessionCount = e.Course.SessionCount,
               ChildID = e.ChildID,
               EnrollmentID = e.EnrollmentID,
               
               Title = e.Course.Title,
               CoachName = e.Course.Coach != null ? e.Course.Coach.Name : "Unassigned",
               SpecialtyName = e.Course.Specialty.Title,
               HourlyCost = e.Course.HourlyCost,
               HourlyCost2 = e.Course.HourlyCost2,
               Status = e.Status,
               ScheduledSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "Scheduled"), // Count all scheduled sessions
               CompletedSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "Completed"), // Count completed sessions
               CanceledSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "Canceled"), // Count all canceled sessions
               OnLeaveSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "OnLeave"), // Count all on leave sessions
               RequestToLeaveSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "RequestToLeave") // Count all requested to leave sessions
           })
           .OrderBy(e => e.CourseID)
           .ToListAsync();
        }

        //Return all children's enrollments with a course
        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(int courseId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Child)
                .Where(e => e.CourseID == courseId && e.EnrollmentID_Ref != null)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseChildAsync(int courseId, int childId, string status)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Child)
                .Include(e => e.Course)
                .Where(e => e.CourseID == courseId && e.ChildID == childId && e.Status == status && e.EnrollmentID_Ref !=null)
                .OrderBy (e => e.ScheduledAt)
                .ToListAsync();
        }


        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseChildAsync(int courseId, int childId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Child)
                .Include(e => e.Course)
                .Where(e => e.CourseID == courseId && e.ChildID == childId && e.EnrollmentID_Ref != null)
                .OrderBy(e => e.ScheduledAt)
                .ToListAsync();
        }

        //public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCoachAsync(int coachId, string status)
        //{
        //    return await _context.CourseEnrollments
        //        .Include(e => e.Child)
        //        .Include(e => e.Child.City)
        //        .Include(e => e.Course.Coach)
        //        .Where(e => e.Course.Coach.CoachID == coachId && e.Status == status)
        //        .OrderByDescending(e => e.CreatedDate)
        //        .ToListAsync();
        //}


        //return the children who registered/canceled/completed courses
        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseAsync(int courseId, string status)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Child)
                .ThenInclude(c => c.City)
                .Where(e => e.CourseID == courseId && e.Status == status && e.EnrollmentID_Ref == null)
                .ToListAsync();
        }

        //open/closed course sessions
        public async Task<IEnumerable<CourseEnrollment>> GetSessionsByCourseAsync(int courseId, string status)
        {
            return await _context.CourseEnrollments
                .Where(e => e.CourseID == courseId && e.Status == status && e.Child==null)
                .ToListAsync();
        }


        //Only return upcoming sessions to delete or edit 
        public async Task<IEnumerable<CourseEnrollment>> GetAllUpcomingSessionsByCourseAsync(int courseId)
        {
            return await _context.CourseEnrollments
                
                .Where(e => e.CourseID == courseId && e.Child==null && e.ScheduledAt >= DateTime.Today)
                .OrderBy(e => e.ScheduledAt) // Sort by ScheduledAt ascending
                .ToListAsync();
        }


        //Only return List of Upcoming Session IDs in a course that are registered 
        public async Task<List<int?>> GetRegisteredUpcomingSessionsByCourseAsync(int courseId)
        {
            return await _context.CourseEnrollments
                .Where(e => e.CourseID == courseId && e.ChildID != null && e.ScheduledAt >= DateTime.Today)
                .OrderBy(e => e.ScheduledAt) // Sort by ScheduledAt ascending
                .Select(e => e.EnrollmentID_Ref)
                .ToListAsync();
        }

        public async Task UpdateChildCompletedSessionsAsync(int courseId)
        {
           
            DateTime now = DateTime.Now;

           //Get all sessions of the course, which Status is 'Scheduled'
            var sessionsToUpdate = await _context.CourseEnrollments
                .Where(e => e.CourseID == courseId
                            && e.Status == "Scheduled"
                            && e.ScheduledAt!=null
                            && e.ScheduledHours != null
                            && ((DateTime)e.ScheduledAt).AddHours((double)e.ScheduledHours) <= now)
                .ToListAsync();

            // Update Status
            foreach (var session in sessionsToUpdate)
            {
                session.Status = "Completed";
            }

            await _context.SaveChangesAsync();
        }


        public async Task UpdateCompletedSessionsAsync(int courseId)
        {

            DateTime now = DateTime.Now;

            //Get all sessions of the course, which Status is 'Scheduled'
            var sessionsToUpdate = await _context.CourseEnrollments
                .Where(e => e.CourseID == courseId
                            && (e.Status == "Open" || e.Status == "Closed")
                            && e.ScheduledAt != null
                            && e.ScheduledHours != null
                            && ((DateTime)e.ScheduledAt).AddHours((double)e.ScheduledHours) <= now)
                .ToListAsync();

            // Update Status
            foreach (var session in sessionsToUpdate)
            {
                session.Status = "Completed";
            }

            await _context.SaveChangesAsync();
        }

    }
}
