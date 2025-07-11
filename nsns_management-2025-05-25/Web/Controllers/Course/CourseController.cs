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

                var result = await _courseService.AddAsync(model.Title, model.Description, model.HourlyCost, model.IsActive, model.CoachID, model.SpecialtyID,user);

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
        public async Task<IActionResult> Edit(int courseId, string title, string description, decimal hourlyCost, bool isActive/*, int userId, int updatedBy*/)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var enrollments = await _courseEnrollmentService.GetScheduledEnrollmentsByCourseAsync(courseId);
                if (enrollments != null && enrollments.Any())
                {
                    TempData["ErrorMessage"] = "This course cannot be set to inactive because it has scheduled sessions.  Please wait until all scheduled sessions completed or deleted all scheduled sessions before deactivating the course.";
                    return RedirectToAction("List");
                }
                    
                var result = await _courseService.UpdateAsync(courseId, title, description, hourlyCost, isActive, user);

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
    }   
}

