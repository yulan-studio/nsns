using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Core.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers.Activity
{
    [Route("Activity")]
    public class ActivityController : Controller
    {

        //[ApiController]

        private readonly IActivityService _activityService;
        private readonly IActivityEnrollmentService _activityEnrollmentService;
        private readonly UserManager<Core.Models.User> _userManager;



        public ActivityController(IActivityService activityService, IActivityEnrollmentService activityEnrollmentService, UserManager<Core.Models.User> userManager)
        {
            _activityService = activityService;
            _activityEnrollmentService = activityEnrollmentService;
            _userManager = userManager;
            _userManager = userManager;
        }


        [Authorize(Roles = "Staff")]
        [HttpGet("ConfirmDelete/{activityId}")]
        public async Task<IActionResult> ConfirmDelete(int activityId)
        {
            // Fetch the staff details from the database
            var activity = await _activityService.GetAsync(activityId);
            if (activity == null)
            {
                return NotFound();
            }

            // Pass the staff details to the Delete.cshtml view
            return View(activity);
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int activityId)
        {
            try
            {
                var result = await _activityService.RemoveAsync(activityId);

                if (!result)
                {
                    TempData["ErrorMessage"] = "The activity could not be deleted.";
                    return RedirectToAction("List");
                }

                TempData["SuccessMessage"] = "The activity has been deleted successfully.";
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

            var activityList = await _activityService.GetAllAsync();
            return View(activityList); // Ensure there is a corresponding List.cshtml in Views/Staff

        }

        //[HttpGet("GetCoachesBySpecialty")]
        //public async Task<IActionResult> GetCoachesBySpecialty(int specialtyId)
        //{
        //    var coaches = await _coachService.GetCoachesBySpecailtyAsync(specialtyId);
        //    return Json(coaches.Select(c => new { c.UserID, c.Name }));
        //}

        //[HttpGet("Add")]
        //public async Task<IActionResult> Add()
        //{
        //    var specialties = await _activityService.GetAllAsync();
        //    ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
        //    {
        //        Value = s.SpecialtyID.ToString(),
        //        Text = s.Title
        //    }).ToList();

        //    return View();
        //}

        [Authorize(Roles = "Staff")]
        [HttpGet("Add")]
        public async Task<IActionResult> Add()
        {
            return View();
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("Add")]
        public async Task<IActionResult> Add(string title, string description, string address, int maxCapacity, DateTime scheduledAt, Decimal cost, /*bool isActive,*/ string status)
        {
            //createdBy = 1; //temparary set

            if (!ModelState.IsValid)
            {
               
                return View();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _activityService.AddAsync( title,  description,  address,  maxCapacity,  scheduledAt,  cost,  status, user);

                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Failed in adding the activity info.");
                   
                    return View();
                }

                TempData["SuccessMessage"] = "Activity info has been added successfully.";
                return RedirectToAction("List"); // Redirect to the course list page
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, $"{ex.Message}");
               
                return View();
            }


        }






        // GET: Edit View
        [Authorize(Roles = "Staff")]
        [HttpGet("Edit/{activityId}")]
        //[HttpGet]
        public async Task<IActionResult> Edit(int activityId)
        {
            // Fetch the staff details from the database
            var activity = await _activityService.GetAsync(activityId);
            if (activity == null)
            {
                return NotFound();
            }

            // Pass the staff details to the Delete.cshtml view
            return View(activity);

        }

        [Authorize(Roles = "Staff")]
        [HttpPost("Edit/{activityId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int activityId, string title, string description, string address, int maxCapacity, DateTime scheduledAt, decimal cost, /*bool isActive,*/ string status)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _activityService.UpdateAsync(activityId,  title,  description,  address,  maxCapacity,  scheduledAt,  cost, /*isActive, */status, user);

                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Failed to update activity information.");
                    var activity = await _activityService.GetAsync(activityId);
                    return View(activity);
                }

                if(status == "Canceled")
                {
                    await _activityEnrollmentService.UpdateActivityStatusToCanceledAsync(activityId);
                }

                TempData["SuccessMessage"] = "Activity information updated successfully.";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                var activity = await _activityService.GetAsync(activityId);
                return View(activity);
            }
        }
    }
}