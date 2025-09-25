using Core.Interfaces;
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

        // GET: Fees/Edit/{courseEnrollmentId}
        [HttpGet]
        public async Task<IActionResult> Edit(int courseEnrollmentId)
        {
            var fee = await _feeService.GetFeeForCourseEnrollmentAsync(courseEnrollmentId);

            if (fee == null)
                return NotFound();

            var model = new FeeEditViewModel
            {
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

                int userId = 1; // TODO: replace with HttpContext.User info

                var success = await _feeService.UpdateFeeAsync(fee, model.Description, model.TotalCost, userId);

                if (!success)
                {
                    ModelState.AddModelError("", "Cannot update a paid fee.");
                    return View(model);
                }

                return RedirectToAction("Details", "CourseEnrollments", new { id = model.CourseEnrollmentID });
            }
            else
            {
                return BadRequest("Invalid fee type.");
            }
                
        }
    }

}
