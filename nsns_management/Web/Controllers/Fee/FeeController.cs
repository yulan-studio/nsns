using Core.Interfaces;
using Core.Models;
using Core.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers.Fee
{
    [Route("Fee")]
    public class FeesController : Controller
    {
        private readonly IFeeService _feeService;
        private readonly UserManager<Core.Models.User> _userManager;

        public FeesController(IFeeService feeService, UserManager<Core.Models.User> userManager)
        {
            _feeService = feeService;
        }

        // GET: Fee/Edit/{courseEnrollmentId}
        [HttpGet]
        public async Task<IActionResult> Edit(int courseEnrollmentId)
        {
            var fee = await _feeService.GetFeeForCourseEnrollmentAsync(courseEnrollmentId);

            if (fee == null)
                return NotFound();

            var model = new FeeEditViewModel
            {
                ChildID = fee.CourseEnrollment.Child.ChildID,
                CourseEnrollmentID = fee.CourseEnrollmentID,
                FeeID = fee.FeeID,
                Description = fee.Description,
                TotalCost = fee.TotalCost,
                IsPaid = fee.IsPaid
            };

            return View(model);
        }

        // POST: Fees/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseFee(FeeEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

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
                    return View(model);
                }

                //return RedirectToAction("Child", "ManageRegistrations", new { id = model. });
                return RedirectToAction("Child", "ManageRegistrations", new { model.ChildID });
            }
            else
            {
                return BadRequest("Invalid fee type.");
            }
                
        }
    }

}
