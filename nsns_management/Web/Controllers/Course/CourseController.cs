using Core.Interfaces;
using Core.Models;
using Core.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers.Courses
{
    [Route("Course")]
    public class CourseController : Controller
    {

        //[ApiController]

        private readonly ICourseService _courseService;
        private readonly ICoachService _coachService;
        private readonly ISpecialtyService _specialtyService;
        private readonly ICourseEnrollmentService _courseEnrollmentService;
        private readonly UserManager<Core.Models.User> _userManager;


        public CourseController(ICourseService courseService, ICourseEnrollmentService courseEnrollmentService, ICoachService coachService, ISpecialtyService specialtyService, UserManager<Core.Models.User> userManager)
        {
            _courseService = courseService;
            _coachService = coachService;
            _specialtyService = specialtyService;
            _userManager = userManager;
            _courseEnrollmentService = courseEnrollmentService;
        }



        [Authorize(Roles = "Staff")]
        [HttpGet("ConfirmDelete/{courseId}")]
        public async Task<IActionResult> ConfirmDelete(int courseId)
        {
            // Fetch the staff details from the database

           
            var course = await _courseService.GetAsync(courseId);
            if(course == null)
            {
                return NotFound();
            }

           
                

                // Pass the staff details to the Delete.cshtml view
            return View(course);
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int courseId)
        {
            try
            {
                var enrollments = await _courseEnrollmentService.GetRegisteredEnrollmentsByCourseAsync(courseId);

                if (enrollments != null && enrollments.Any())
                {
                    TempData["ErrorMessage"] = "This course cannot be deleted because it has enrolled students. Please try editing the course and set it to inactive.";
                    return RedirectToAction("List"); // Redirect to the course list page
                }


                var result = await _courseService.RemoveAsync(courseId);

                if (!result)
                {
                    TempData["ErrorMessage"] = "The course could not be deleted.";
                    return RedirectToAction("List");
                }

                TempData["SuccessMessage"] = "The course has been deleted successfully.";
                return RedirectToAction("List"); // Redirect to the course list page
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("List"); // Redirect to the course list page
            }

            // If delete fails, reload the confirmation page


        }


        [Authorize(Roles = "Staff")]
        [HttpGet("ConfirmDeleteSession/{enrollmentId}")]
        public async Task<IActionResult> ConfirmDeleteSession(int enrollmentId)
        {

            var session = await _courseEnrollmentService.GetAsync(enrollmentId);
            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("DeleteSessionConfirmed")]
        public async Task<IActionResult> DeleteSessionConfirmed(int enrollmentId)
        {
            try
            {
                //var enrollments = await _courseEnrollmentService.GetRegisteredEnrollmentsByCourseAsync(courseId);

                //if (enrollments != null && enrollments.Any())
                //{
                //    TempData["ErrorMessage"] = "This course cannot be deleted because it has enrolled students. Please try editing the course and set it to inactive.";
                //    return RedirectToAction("List"); // Redirect to the course list page
                //}


                var result = await _courseEnrollmentService.RemoveAsync(enrollmentId);

                if (!result)
                {
                    TempData["ErrorMessage"] = "The session could not be deleted.";
                    return RedirectToAction("List");
                }

                TempData["SuccessMessage"] = "The session has been deleted successfully.";
                return RedirectToAction("List"); // Redirect to the course list page
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("List"); // Redirect to the course list page
            }

            // If delete fails, reload the confirmation page


        }


        // GET: Add View
        [Authorize(Roles = "Admin, Staff")]
        [HttpGet("List")]
        //[HttpGet]
        public async Task<IActionResult> List()
        {

            var courseList = await _courseService.GetAllAsync();
            return View(courseList); // Ensure there is a corresponding List.cshtml in Views/Staff

        }

        [HttpGet("GetCoachesBySpecialty")]
        public async Task<IActionResult> GetCoachesBySpecialty(int specialtyId)
        {
            var coaches = await _coachService.GetCoachesBySpecailtyAsync(specialtyId);
            return Json(coaches.Select(c => new { c.CoachID, c.Name }));
        }

        [Authorize(Roles = "Staff")]
        [HttpGet("Add")]
        public async Task<IActionResult> Add()
        {
            var specialties = await _specialtyService.GetAllAsync();
            ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
            {
                Value = s.SpecialtyID.ToString(),
                Text = s.Title
            }).ToList();

            return View();
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("Add")]
        public async Task<IActionResult> Add(AddCourseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var specialties = await _specialtyService.GetAllAsync();
                ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
                {
                    Value = s.SpecialtyID.ToString(),
                    Text = s.Title
                }).ToList();
                return View();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                var result = await _courseService.AddAsync(model.Title, model.Description,model.CourseType, model.MaxCapacity, model.SessionCount, model.HourlyCost, model.HourlyCost2, model.IsActive, model.CoachID, model.SpecialtyID, user);

                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Failed in adding the course info.");
                    var specialties = await _specialtyService.GetAllAsync();
                    ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
                    {
                        Value = s.SpecialtyID.ToString(),
                        Text = s.Title
                    }).ToList();
                    return View();
                }

                TempData["SuccessMessage"] = "Course info has been added successfully.";
                return RedirectToAction("List"); // Redirect to the course list page
            }
            catch (Exception ex)
            {
               
                ModelState.AddModelError(string.Empty, $"{ex.Message}");
                var specialties = await _specialtyService.GetAllAsync();
                ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
                {
                    Value = s.SpecialtyID.ToString(),
                    Text = s.Title
                }).ToList();
                return View(); 
            }

                
        }






        // GET: Edit View
        [Authorize(Roles = "Staff")]
        [HttpGet("Edit/{courseId}")]
        //[HttpGet]
        public async Task<IActionResult> Edit(int courseId)
        {
            // Fetch the staff details from the database
            var course = await _courseService.GetAsync(courseId);
            if (course == null)
            {
                return NotFound();
            }

            // Pass the staff details to the Delete.cshtml view
            return View(course);

        }

        [Authorize(Roles = "Staff")]
        [HttpPost("Edit/{courseId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int courseId, string title, string description, string courseType, int? maxCapacity, int? sessionCount, decimal hourlyCost, decimal hourlyCost2, bool isActive/*, int userId, int updatedBy*/)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var enrollments = await _courseEnrollmentService.GetScheduledEnrollmentsByCourseAsync(courseId);
                if (enrollments != null && enrollments.Any() && isActive == false)
                {
                    TempData["ErrorMessage"] = "This course cannot be set to inactive because it has scheduled sessions.  Please wait until all scheduled sessions completed or deleted all scheduled sessions before deactivating the course.";
                    return RedirectToAction("List");
                }

               

                var result = await _courseService.UpdateAsync(courseId, title, description, courseType, maxCapacity, sessionCount, hourlyCost, hourlyCost2, isActive, user);

                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Failed to update course information.");
                    var course = await _courseService.GetAsync(courseId);
                    return View(course);
                }

                TempData["SuccessMessage"] = "Course information updated successfully.";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                //TempData["ErrorMessage"] = $"{ex.Message}";
                ModelState.AddModelError(string.Empty, $"{ex.Message}");
                var course = await _courseService.GetAsync(courseId);
                return View(course);
            }
        }

        [Authorize(Roles = "Staff")]
        [HttpGet("ManageSessions/{courseId}")]
        public async Task<IActionResult> ManageSessions(int courseId)
        {
            //var course = _context.Courses.FirstOrDefault(c => c.CourseID == courseId);
            //if (course == null || course.CourseType != "Group")
            //{
            //    TempData["ErrorMessage"] = "Invalid course.";
            //    return RedirectToAction("Index");
            //}

            //var sessions = _context.Course_Enrollments
            //    .Where(e => e.CourseID == courseId && e.Status == "Open")
            //    .OrderBy(e => e.ScheduledAt)
            //    .ToList();

            Course course = await _courseService.GetAsync(courseId);
            if (course == null || course.CourseType != "Group")
            {
                TempData["ErrorMessage"] = "Invalid course.";
                return RedirectToAction("List");
            }

            var openSessions = await _courseEnrollmentService.GetOpenSessionsByCourseAsync(courseId);
            var closedSessions = await _courseEnrollmentService.GetClosedSessionsByCourseAsync(courseId);
            var canceledSessions = await _courseEnrollmentService.GetCanceledSessionsByCourseAsync(courseId);
            var completedSessions = await _courseEnrollmentService.GetCompletedSessionsByCourseAsync(courseId);

            var allSessions = await _courseEnrollmentService.GetAllSessionsByCourseAsync(courseId);

            ViewBag.CourseID = courseId;

           

            var model = new ManageSessionsViewModel
            {
                Course = course,
                OpenSessions = (List<CourseEnrollment>?)openSessions,
                CompletedSessions = (List<CourseEnrollment>?)completedSessions,
                CanceledSessions = (List<CourseEnrollment>?)canceledSessions,
                ClosedSessions = (List<CourseEnrollment>?)closedSessions,
                AllSessions = (List<CourseEnrollment>?)allSessions

            };

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> AddSession(int courseId, DateTime scheduledAt, decimal scheduledHours, string location, string staffNote)
        {
            var user = await _userManager.GetUserAsync(User);

            Course course = await _courseService.GetAsync(courseId);
            if (course == null || course.CourseType != "Group")
            {
                TempData["ErrorMessage"] = "Invalid course.";
                return RedirectToAction("ManageSessions", new { courseId });
            
            }

            try
            {
                var result = await _courseEnrollmentService.AddSessionToGroupCourseAsync(courseId, scheduledAt, scheduledHours, location, staffNote, user);
                if (!result)
                {
                    TempData["ErrorMessage"] = "New session has problem to be added.";
                }
                TempData["SuccessMessage"] = "New session added successfully.";
                return RedirectToAction("ManageSessions", new { courseId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("ManageSessions", new { courseId });
            }

           
        }


    }   
}

