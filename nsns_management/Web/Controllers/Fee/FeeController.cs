using Core.Interfaces;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

namespace Web.Controllers.Fee
{
    [Route("Fee")]
    public class FeeController : Controller
    {
        private readonly IFeeService _feeService;
        private readonly UserManager<Core.Models.User> _userManager;

        public FeeController(IFeeService feeService, UserManager<Core.Models.User> userManager)
        {
            _feeService = feeService;
            _userManager = userManager;
        }

        // GET: Fee/Edit/{courseEnrollmentId}
        
        [HttpGet("Edit/{courseEnrollmentId}")]
        public async Task<IActionResult> Edit(int courseEnrollmentId)
        {
            var fee = await _feeService.GetFeeForCourseEnrollmentAsync(courseEnrollmentId);

            if (fee == null)
                return NotFound();

            var feeModel = new FeeEditViewModel
            {
                ChildID = fee.CourseEnrollment.Child.ChildID,
                CourseEnrollmentID = fee.CourseEnrollmentID,
                FeeID = fee.FeeID,
                Description = fee.Description,
                TotalCost = fee.TotalCost,
                IsPaid = fee.IsPaid
            };

            //return View(model);
            return PartialView("_Edit", feeModel);
        }


        



        // POST: Fees/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseFee(FeeEditViewModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_Edit", model);

            var user = await _userManager.GetUserAsync(User);

            if (model.CourseEnrollmentID != null)
            { // Ensure it's a course fee
                var fee = await _feeService.GetFeeForCourseEnrollmentAsync((int)model.CourseEnrollmentID);

                if (fee == null)
                    return NotFound();

                //int userId = 1; // TODO: replace with HttpContext.User info
                int userId = user.Id;

                var success = await _feeService.UpdateFeeAsync(fee, model.Description, model.TotalCost, userId);

                if (!success)
                {
                    ModelState.AddModelError("", "Cannot update a paid fee.");
                    return PartialView("_Edit", model);
                }

                //return RedirectToAction("Child", "ManageRegistrations", new { id = model. });
                return RedirectToAction("ManageRegistrations", "Child", new { childId = model.ChildID });
            }
            else
            {
                return BadRequest("Invalid fee type.");
            }
                
        }
    }

}
