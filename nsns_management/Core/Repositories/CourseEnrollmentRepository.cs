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

        //Get Registered/Scheduled/completed/request to schedule/request to leave/deleted group or private course session
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

        //Get Registered/Completed records of root course registration
        public async Task<IEnumerable<CourseEnrollment>> GetRootEnrollmentsByChildAsync(int childId, string status)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Course)
                .Include(e => e.Course.Coach)
                .Include(e => e.Course.Specialty)
                .Where(e => e.ChildID != null && e.ChildID == childId && e.Status == status)
                .OrderBy(e => e.CourseID)
                .OrderBy(e => e.ScheduledAt)
                .ToListAsync();
        }
        //Return Group Course Schedule/Deleted sessions, this is the status of the group sessions before confirming
        public async Task<IEnumerable<CourseEnrollment>> GetScheduledSessionsToConfirmByChildAsync(int childId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Course)
                .Include(e => e.Course.Coach)
                .Include(e => e.Course.Specialty)
                .Where(e => e.ChildID != null && e.ChildID == childId && (e.Status == "Registered") && e.Course.CourseType=="Group" && e.EnrollmentID_Ref != null)
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

        //Include /Scheduled/RequestToReschedule/RequestToCancel/Canceled/Deleted (not include Registered, Completed )  for private course
        //Include /Scheduled/RequestToReschedule/RequestToCancel/Canceled (not include Registered, Completed, Deleted ) for group courses
        public async Task<IEnumerable<CourseEnrollment>> GetUpcomingEnrollmentsByChildAsync(int childId)
        {
            var torontoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var torontoNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, torontoTimeZone);
            return await _context.CourseEnrollments
                .Include(e => e.Course)
                .Include(e => e.Course.Coach)
                .Include(e => e.Course.Specialty)
                .Where(e => e.ChildID != null && e.ChildID == childId && e.EnrollmentID_Ref != null && (e.Status != "Registered" && e.Status != "Completed" && e.Status != "Deleted"|| (e.Course.CourseType == "Private" && e.Status == "Deleted"))&& e.ScheduledAt>= torontoNow)
                .OrderBy(e => e.CourseID)
                .OrderBy(e => e.ScheduledAt)
                .ToListAsync();
        }


        //Get registered and completed course information, so that these course won't be registered again
        //Get Registered courses for a child, include number of scheduled sessions, number of completed sessions, number of canceled sessions, number of on leave sessions, number of request to reschedule sessions, number of request to leave sessions
        public async Task<IEnumerable<CourseEnrollmentViewModel>> GetRegisteredEnrollmentsByChildAsync(int childId)  
        {
            return await _context.CourseEnrollments
           .Include(e => e.Course)
           .Include(e => e.Course.Coach)
           .Include(e => e.Course.Specialty)
           .Where(e => e.ChildID == childId && ((e.Status == "Registered" || e.Status == "Completed" ) && e.EnrollmentID_Ref == null))  //Not included those registered to session
           .OrderBy(e => e.CreatedDate)
           .Select(e => new CourseEnrollmentViewModel
           {
               
               CourseID = e.CourseID,
               CourseType = e.Course.CourseType,
               IsActive = e.Course.IsActive,
               SessionCount = e.Course.SessionCount,
               ChildID = e.ChildID,
               EnrollmentID = e.EnrollmentID,
               
               Title = e.Course.Title,
               CoachName = e.Course.Coach != null ? e.Course.Coach.Name : "N/A",
               SpecialtyName = e.Course.Specialty.Title,
               HourlyCost = e.Course.HourlyCost,
               HourlyCost2 = e.Course.HourlyCost2,
               Status = e.Status,
               ScheduledSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "Scheduled" && c.EnrollmentID_Ref != null), // Count all scheduled sessions
               CompletedSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "Completed" && c.EnrollmentID_Ref != null), // Count completed sessions
               CanceledSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "Canceled" && c.EnrollmentID_Ref != null), // Count all canceled sessions
               OnLeaveSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "OnLeave" && c.EnrollmentID_Ref != null), // Count all on leave sessions
               RequestToLeaveSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "RequestToLeave" && c.EnrollmentID_Ref != null), // Count all requested to leave sessions,
               RequestToRescheduleSessions = _context.CourseEnrollments.Count(c => c.ChildID == e.ChildID && c.CourseID == e.CourseID && c.Status == "RequestToReschedule" && c.EnrollmentID_Ref != null) // Count all requested to leave sessions
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

        //Include Registered/Scheduled/Canceled/Completed/RequestToReschedule/RequestToCancel (not include Deleted)
        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseChildAsync(int courseId, int childId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Child)
                .Include(e => e.Course)
                .Where(e => e.CourseID == courseId && e.ChildID == childId && e.EnrollmentID_Ref != null && e.Status != "Deleted")
                .OrderBy(e => e.ScheduledAt)
                .ToListAsync();
        }



        //Include /Scheduled/RequestToReschedule/RequestToCancel/Canceled/Deleted (not include Registered, Completed )
        public async Task<IEnumerable<CourseEnrollment>> GetUpcomingEnrollmentsByCourseChildAsync(int courseId, int childId)
        {
            var torontoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var torontoNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, torontoTimeZone);
            return await _context.CourseEnrollments
                .Include(e => e.Child)
                .Include(e => e.Course)
                .Where(e => e.CourseID == courseId && e.ChildID == childId && e.EnrollmentID_Ref != null && e.Status != "Registered" && e.Status != "Completed" && e.ScheduledAt>=torontoNow)
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
                .OrderBy(e => e.ScheduledAt) // Sort by ScheduledAt ascending
                .ToListAsync();
        }


        //Only return upcoming sessions to delete or edit 
        public async Task<IEnumerable<CourseEnrollment>> GetAllUpcomingSessionsByCourseAsync(int courseId)
        {
            var torontoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var torontoNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, torontoTimeZone);
            return await _context.CourseEnrollments
                
                .Where(e => e.CourseID == courseId && e.Child==null && e.ScheduledAt >= torontoNow)
                .OrderBy(e => e.ScheduledAt) // Sort by ScheduledAt ascending
                .ToListAsync();
        }


        //Only return List of Upcoming Session IDs in a course that are registered 
        public async Task<List<int?>> GetRegisteredUpcomingSessionsByCourseAsync(int courseId)
        {
            var torontoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var torontoNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, torontoTimeZone);
            return await _context.CourseEnrollments
                .Where(e => e.CourseID == courseId && e.ChildID != null && e.ScheduledAt >= torontoNow)
                .OrderBy(e => e.ScheduledAt) // Sort by ScheduledAt ascending
                .Select(e => e.EnrollmentID_Ref)
                .ToListAsync();
        }

        //return a list of ChildIDs where at least one course enrollment session has Status == "RequestToLeave":
        public async Task<List<int?>> GetChildrenWithRequestToLeaveAsync()
        {
            return await _context.CourseEnrollments
                .Where(e => e.EnrollmentID_Ref != null && e.Status == "RequestToLeave")
                .Select(e => e.ChildID)
                .Distinct()
                .ToListAsync();
        }


        //Here's how you can create a method to return a list of children who:
        //Have a group course session(EnrollmentID_Ref != null)
        //Status is "Registered"
        //ParentNote is not null or empty(indicating a concern about the scheduled time)
        public async Task<List<int?>> GetChildrenWithScheduleConcernsAsync()
        {
            return await _context.CourseEnrollments
                .Where(e =>
                    e.EnrollmentID_Ref != null &&
                    e.Status == "Registered" &&
                    !string.IsNullOrEmpty(e.ParentNote))
                .Select(e => e.ChildID)
                .Distinct()
                .ToListAsync();
        }

        //This is for staff to get all children who have either a request to leave or a concern about their schedule
        public async Task<List<int?>> GetChildrenWithRequestsOrConcernsAsync()
        {
            var requestToLeaveChildren = await GetChildrenWithRequestToLeaveAsync();
            var scheduleConcernChildren = await GetChildrenWithScheduleConcernsAsync();

            // Combine both lists and eliminate duplicates
            var combined = requestToLeaveChildren
                .Union(scheduleConcernChildren)
                .ToList();

            return combined;
        }



        //For Group Courses Set children's sessions to be completed when it's finished
        public async Task UpdateChildCompletedSessionsAsync(int courseId)
        {
           
            DateTime now = DateTime.UtcNow;

           //Get all sessions of the course, which Status is 'Scheduled'
            var sessionsToUpdate = await _context.CourseEnrollments
                .Include(e=>e.Course)
                .Where(e => e.CourseID == courseId
                            && e.Course.CourseType == "Group"
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



        //For Group Courses, Set sessions to be completed when it's finished
        public async Task UpdateCompletedSessionsAsync(int courseId)
        {

            DateTime now = DateTime.UtcNow;

            //Get all sessions of the group course, which Status is 'Scheduled'
            var sessionsToUpdate = await _context.CourseEnrollments
                .Include(e => e.Course)
                .Where(e => e.CourseID == courseId
                            && (e.Course.CourseType == "Group")
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


        //Set all registration related to a enrollmentId to be Cancled.
        public async Task UpdateChildCanceledSessionsAsync(int enrollmentId)
        {



            //Get all registered sessions of the course session, which Status is 'Scheduled' or 'Registered'
            var sessionsToUpdate = await _context.CourseEnrollments
                .Where(e => e.EnrollmentID_Ref == enrollmentId
                            && (e.Status == "Scheduled" || e.Status == "Registered")
                            && e.ScheduledAt != null
                            && e.ScheduledHours != null)
                .ToListAsync();

            // Update Status
            foreach (var session in sessionsToUpdate)
            {
                session.Status = "Canceled";
            }

            await _context.SaveChangesAsync();
        }



        public async Task<bool> UpdateCompletedCoursesAsync()
        {
            var updated = false;

            // Step 1: Get all root course enrollments for private and group courses (not per-session enrollments)
            var rootEnrollments = await _context.CourseEnrollments
                .Include(e => e.Course)
                .Where(e =>
                    e.Status == "Registered" &&
                    e.EnrollmentID_Ref == null &&
                    e.ScheduledAt == null)
                .ToListAsync();
            //Step 2: Get all Completed sessions for all root course, check completed count
            foreach (var root in rootEnrollments)
            {
                int completedSessions = await _context.CourseEnrollments
                    .Where(c =>
                        c.ChildID == root.ChildID &&
                        c.CourseID == root.CourseID &&
                        c.EnrollmentID_Ref != null &&
                        c.Status == "Completed") // We only want to change "registered" sessions to "Completed"
                    .CountAsync();

                //if completed count equal to session count
                if (completedSessions == root.Course.SessionCount)
                {
                    //Step 3: Fetch those registered sessions to update them

                    //var registrationToUpdate = await _context.CourseEnrollments
                    //    .Where(c =>
                    //        c.ChildID == root.ChildID &&
                    //        c.CourseID == root.CourseID &&
                    //        c.EnrollmentID_Ref == null &&
                    //        c.ScheduledAt == null &&
                    //        c.Status == "Registered")
                    //    .ToListAsync();

                    //foreach (var registration in registrationToUpdate)
                    //{
                    //    registration.Status = "Completed";
                    //}

                    root.Status = "Completed";

                    updated = true;
                }
            }

            if (updated)
            {
                await _context.SaveChangesAsync();
            }

            return updated;
        }




    }
}
