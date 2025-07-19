
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Core.Interfaces;
using Core.Models;
using Core.ViewModels;

using System.Diagnostics;
using Core.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http.HttpResults;
using static Core.ViewModels.ManageSessionRegistrationsViewModel;
using Core.FormModels;




namespace Web.Controllers.User
{


    [Route("Child")]

    //[ApiController]
    public class ChildController : Controller
    {
        private readonly IChildService _childService;
        private readonly ICourseService _courseService;
        private readonly IParentService _parentService;
        private readonly ICityService _cityService;
        private readonly IParentChildService _parentChildService;
        private readonly ICourseEnrollmentService _courseEnrollmentService;
        private readonly ISpecialtyService _specialtyService;
        private readonly IActivityEnrollmentService _activityEnrollmentService;
        private readonly IActivityService _activityService;
        private readonly IPaymentService _paymentService;
        private readonly IChildBalanceService _balanceService;
        private readonly UserManager<Core.Models.User> _userManager;

        public ChildController(IChildService childService, ICourseService courseService, IChildBalanceService balanceService, IParentService parentService, ICityService cityService, IParentChildService parentChildService, ISpecialtyService specialtyService, IActivityService activityService, ICourseEnrollmentService courseEnrollmentService, IActivityEnrollmentService activityEnrollmentService, IPaymentService paymentService, UserManager<Core.Models.User> userManager)
        {
            _childService = childService;
            _balanceService = balanceService;
            _parentService = parentService;
            _cityService = cityService;
            _parentChildService = parentChildService;
            _courseService = courseService;
            _specialtyService = specialtyService;
            _courseEnrollmentService = courseEnrollmentService;
            _activityService = activityService;
            _activityEnrollmentService = activityEnrollmentService;
            _paymentService = paymentService;
            _userManager = userManager;
        }

        // ✅ Helper method to get City List
        private async Task<List<SelectListItem>> GetCityList()
        {
            return (await _cityService.GetAllAsync())
                .Select(c => new SelectListItem { Value = c.CityID.ToString(), Text = c.Name })
                .ToList();
        }


        private async Task<List<SelectListItem>> GetCityList(Child child)
        {

            var cities = await _cityService.GetAllAsync();

            return cities.Select(c => new SelectListItem
            {
                Value = c.CityID.ToString(),
                Text = c.Name,
                Selected = c.CityID == child.CityID
            }).ToList();
        }


        [HttpGet("List")]
        // ✅ List all children
        public async Task<IActionResult> List()
        {
            var childrenWithRequestOrConcerns = await _courseEnrollmentService.GetChildrenWithRequestsOrConcernsAsync();
            //ViewBag.RequestConcernChildIds = childrenWithConcerns;
            ViewData["RequestConcernChildIds"] = childrenWithRequestOrConcerns;
            var childrenWithDelete = new List<ChildWithDeleteViewModel>();
            var children = await _childService.GetAllAsync();
            foreach (Child c in children)
            {
                var canDelete = !await _childService.CheckPaidAsync(c.ChildID) && !await _childService.CheckRegisteredAsync(c.ChildID);
                var childWithDelete = new ChildWithDeleteViewModel();
                childWithDelete.Child = c;
                childWithDelete.CanDelete = canDelete;
                childrenWithDelete.Add(childWithDelete);
            }

            return View(childrenWithDelete);


            //var children = await _childService.GetAllAsync();
            //ViewBag.ParentList = (await _parentService.GetAllParentsAsync())
            //    .Select(p => new SelectListItem { Value = p.ParentID.ToString(), Text = p.Name })
            //    .ToList();

            //return View(children);

        }


        // GET: Add View
        [HttpGet("Add")]
        //[HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewBag.CityList = await GetCityList();

            return View();
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("Add")]
        public async Task<IActionResult> Add(string name, DateTime birthDate, string gender, int cityId, string email, string password, bool hasOAP)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                Core.Models.User user = await _userManager.GetUserAsync(User);
                var result = await _childService.AddAsync(name, birthDate, gender, cityId, email, password, hasOAP, user);
                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Failed in adding the child info.");
                    ViewBag.CityList = await GetCityList();
                    return View();
                }

                TempData["SuccessMessage"] = "Child info has been added successfully.";
                return RedirectToAction("List"); // Redirect to the child list page


            }
            catch (Exception ex)
            {
                ModelState.AddModelError(String.Empty, $"{ex.Message}");
                ViewBag.CityList = await GetCityList();
                return View();

            }


            //City city = cityId.HasValue ? await _cityService.GetAsync(cityId) : null;
            ////child.City = city;
            ////child.Role = "Child";
            //try
            //{
            //    var result = await _childService.AddAsync(child);
            //    if (!result)
            //    {
            //        ModelState.AddModelError(string.Empty, "Failed in adding the coach info.");


            //        // Repopulate CityList for the dropdown if validation fails

            //        ViewBag.CityList = await GetCityList();
            //        return View();
            //    }
            //    TempData["SuccessMessage"] = "Child info has been added successfully.";
            //    return RedirectToAction("List"); // Redirect to the child list page

            //}

            //catch (Exception ex)
            //{
            //    ModelState.AddModelError(String.Empty, $"Error: {ex.Message}");
            //    // Repopulate CityList for the dropdown if validation fails
            //    ViewBag.CityList = await GetCityList();
            //    return View();
            //}
        }

        [Authorize(Roles = "Staff")]
        // ✅ GET: Show Edit form with Child data
        [HttpGet("Edit/{childId}")]
        public async Task<IActionResult> Edit(int childId)
        {
            var child = await _childService.GetAsync(childId);
            if (child == null)
            {
                TempData["ErrorMessage"] = "Child not found.";
                return RedirectToAction("List");
            }

            ViewBag.CityList = await GetCityList(child);
            return View(child);
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("Edit/{childId}")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int childId, string name, DateTime birthDate, string gender, int cityId, string email, bool hasOAP/*, string password*/)
        {


            try
            {
                var result = await _childService.UpdateAsync(childId, name, birthDate, gender, cityId, email, hasOAP/*, string password*/);


                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Failed to update child information.");
                    var child = await _childService.GetAsync(childId);

                    if (child == null)
                    {
                        return NotFound();
                    }


                    ViewBag.CityList = await GetCityList(child);

                    // Pass the coach details to the Edit.cshtml view
                    return View(child);
                }

                TempData["SuccessMessage"] = "Child information updated successfully.";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                //TempData["ErrorMessage"] = $"Error: {ex.Message}";
                var child = await _childService.GetAsync(childId);

                if (child == null)
                {
                    return NotFound();
                }

                ViewBag.CityList = await GetCityList(child);

                // Pass the child details to the Edit.cshtml view
                return View(child);
            }
        }



        [Authorize(Roles = "Staff")]
        // ✅ GET: Confirm delete page
        [HttpGet("ConfirmDelete/{childId}")]
        public async Task<IActionResult> ConfirmDelete(int childId)
        {
            var child = await _childService.GetAsync(childId);
            if (child == null)
            {
                TempData["ErrorMessage"] = "Child not found.";
                return RedirectToAction("List");
            }

            return View(child);
        }

        [Authorize(Roles = "Staff")]
        // ✅ POST: Delete Child
        [HttpPost("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int childId)
        {
            var success = await _childService.RemoveAsync(childId);
            if (!success)
            {
                TempData["ErrorMessage"] = "Failed to delete child.";
            }
            else
            {
                TempData["SuccessMessage"] = "Child deleted successfully.";
            }

            return RedirectToAction("List");
        }


        [Authorize(Roles = "Staff")]
        [HttpGet("ManageParents/{childId}")]
        public async Task<IActionResult> ManageParents(int childId)
        {
            var child = await _childService.GetAsync(childId);
            if (child == null)
            {
                TempData["ErrorMessage"] = "Child not found.";
                return RedirectToAction("List");
            }

            var parents = await _parentChildService.GetParentsByChildIdAsync(childId);


            var model = new ManageParentsViewModel
            {
                Child = child,
                Parents = parents
            };

            return View(model);
        }

        //[Authorize(Roles = "Staff")]
        //[HttpPost("AddParentToChild")]
        //public async Task<IActionResult> AddParentToChild(int childId, string parentName, string relationship, string phone,string email, string wechat)
        //{
        //    try
        //    {
        //        // ✅ 1. Create a new Parent object
        //        var user = await _userManager.GetUserAsync(User);
        //        var newParent = new Parent
        //        {
        //            Name = parentName,
        //            Phone = phone,
        //            Email = email,
        //            Wechat = wechat,
        //            CreatedBy = user.Id, // Assume the user ID of admin/creator
        //            CreatedDate = DateTime.UtcNow
        //        };

        //        // ✅ 2. Save the parent in the database
        //        var parentId = await _parentService.AddAndReturnIdAsync(newParent);
        //        if (parentId == 0)
        //        {
        //            TempData["ErrorMessage"] = "Failed to add parent.";
        //            return RedirectToAction("ManageParents", new { childId });
        //        }


        //        var success = await _parentChildService.AddParentToChild(parentId, newParent, childId, relationship, user.Id); // Assuming CreatedBy = 1
        //        if (!success)
        //        {
        //            TempData["ErrorMessage"] = "Failed to add parent to child.";
        //        }
        //        else
        //        {
        //            TempData["SuccessMessage"] = "Parent added to child successfully.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = $"{ex.Message}";
        //    }

        //    return RedirectToAction("ManageParents", new { childId });
        //}

        //[Authorize(Roles = "Staff")]
        //[HttpPost("RemoveParentFromChild")]
        //public async Task<IActionResult> RemoveParentFromChild(int parentChildId, int childId, int parentId)
        //{
        //    try
        //    {

        //        var success = await _parentChildService.RemoveParentFromChild(parentChildId);
        //        success = await _parentService.DeleteAsync(parentId);
        //        if (!success)
        //        {
        //            TempData["ErrorMessage"] = "Failed to remove parent.";
        //        }
        //        else
        //        {
        //            TempData["SuccessMessage"] = "Parent removed successfully.";
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = $"{ex.Message}";
        //    }

        //    return RedirectToAction("ManageParents", new { childId });
        //}


        [Authorize(Roles = "Staff")]
        [HttpGet("AddParent")]
        public async Task<IActionResult> AddParent(int childId)
        {
            ViewBag.ChildId = childId;
            return View();
        }






        [Authorize(Roles = "Staff")]
        [HttpPost("AddParent")]
        public async Task<IActionResult> AddParent(int childId, string parentName, string relationship, string phone, string email, string wechat)
        {
            try
            {
                // ✅ 1. Create a new Parent object
                var user = await _userManager.GetUserAsync(User);
                var newParent = new Parent
                {
                    Name = parentName,
                    Phone = phone,
                    Email = email,
                    Wechat = wechat,
                    CreatedBy = user.Id, // Assume the user ID of admin/creator
                    CreatedDate = DateTime.UtcNow
                };

                // ✅ 2. Save the parent in the database
                var parentId = await _parentService.AddAndReturnIdAsync(newParent);
                if (parentId == 0)
                {
                    TempData["ErrorMessage"] = "Failed to add parent.";
                    return RedirectToAction("ManageParents", new { childId });
                }


                var success = await _parentChildService.AddParentToChild(parentId, newParent, childId, relationship, user.Id); // Assuming CreatedBy = 1
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to add parent to child.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Parent added to child successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
            }

            return RedirectToAction("ManageParents", new { childId });
        }

        [Authorize(Roles = "Staff")]
        [HttpGet("EditParent /{parentId}")]
        public async Task<IActionResult> EditParent(int childId, int parentId/*, string parentName, string relationship, string phone, string email, string wechat*/)
        {
            ViewBag.ChildId = childId;
            Parent parent = await _parentService.GetParentByIdAsync(parentId);
            return View(parent);
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("EditParent")]
        //public async Task<IActionResult> EditParent(int parentId, string parentName, string relationship, string phone, string email, string wechat)
        public async Task<IActionResult> EditParent(Parent parent)
        {
            //ViewBag.ParentId = parentId;
            var childId = ViewBag.ChildId;
            //Parent parent = await _parentService.GetParentByIdAsync(parentId);
            try
            {
                var result = await _parentService.UpdateAsync(parent);
                if (result == true)
                {
                    TempData["SuccessMessage"] = "Parent info updated";
                }
                else
                {
                    TempData["ErrorMessage"] = "Parent info not updated";
                }
                //return RedirectToAction("ManageParents");
                return RedirectToAction("ManageParents", new { childId });
            }

            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("ManageParents", new { childId });
            }
        }
            
       


        [Authorize(Roles = "Staff")]
        [HttpGet("ConfirmDeleteParent/{parentId}")]
        public async Task<IActionResult> ConfirmDeleteParent(int parentChildId, int childId, int parentId)
        {
            try
            {
                ViewBag.ChildId = childId;
                ViewBag.ParentChildId = parentChildId;

                var parent = await _parentService.GetParentByIdAsync(parentId);
                return View(parent);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("ManageParents", new { childId });
            }
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("DeleteParentConfirmed")]
        public async Task<IActionResult> DeleteParentConfirmed(int parentChildId, int childId, int parentId)
        {
            try
            {

                var success = await _parentChildService.RemoveParentFromChild(parentChildId);
                success = await _parentService.DeleteAsync(parentId);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to remove parent.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Parent removed successfully.";
                }
                return RedirectToAction("ManageParents", new { childId });

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("ManageParents", new { childId });
            }

        }

        [Authorize(Roles = "Staff")]
        [HttpGet("ManageRegistrations/{childId}")]
        public async Task<IActionResult> ManageRegistrations(int childId)
        {
            var childrenWithConcerns = await _courseEnrollmentService.GetChildrenWithConcernsAsync();
            //ViewBag.RequestConcernChildIds = childrenWithConcerns;
            ViewData["ConcernChildIds"] = childrenWithConcerns;


            var child = await _childService.GetAsync(childId);
            if (child == null)
            {
                TempData["ErrorMessage"] = "Child not found.";
                return RedirectToAction("List"); // Redirect to child list page if not found
            }

            //Get Registered and Completed Courses
            var courseEnrollments = await _courseEnrollmentService.GetRegisteredEnrollmentsByChildAsync(childId);
            var specialties = await _specialtyService.GetAllAsync();

            ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
            {
                Value = s.SpecialtyID.ToString(),
                Text = s.Title
            }).ToList();

            var activityRegisteredEnrollments =  await _activityEnrollmentService.GetRegisteredEnrollmentsByChildAsync(childId);
            var activityCanceledEnrollments =  await _activityEnrollmentService.GetCanceledEnrollmentsByChildAsync(childId);
            var activityEnrollments = activityRegisteredEnrollments.Concat(activityCanceledEnrollments);
            var activities = await _activityService.GetAllActiveOpenAsync();

            ViewBag.ActivityList = activities.Select(a => new SelectListItem
            {
                Value = a.ActivityID.ToString(),
                Text = a.Title
            }).ToList();

            return View("ManageRegistrations", new ManageRegisterationsViewModel
            {
                Child = child,
                CourseEnrollments = courseEnrollments,
                ActivityEnrollments = activityEnrollments
            });
        }



        [Authorize(Roles = "Child")]
        [HttpGet("MyRegistrations")]
        public async Task<IActionResult> MyRegistrations()
        {
            Core.Models.User user = await _userManager.GetUserAsync(User);
            var child = await _childService.GetByIdAsync(user.Id);

            //Get Registered and Completed Courses
            var courseEnrollments = await _courseEnrollmentService.GetRegisteredEnrollmentsByChildAsync(child.ChildID);
            

            var activityRegisteredEnrollments = await _activityEnrollmentService.GetRegisteredEnrollmentsByChildAsync(child.ChildID);
            var activityCanceledEnrollments = await _activityEnrollmentService.GetCanceledEnrollmentsByChildAsync(child.ChildID);
            var activityEnrollments = activityRegisteredEnrollments.Concat(activityCanceledEnrollments);

           
            return View("MyRegistrations", new ManageRegisterationsViewModel
            {
                Child = child,
                CourseEnrollments = courseEnrollments,
                ActivityEnrollments = activityEnrollments
            });
        }




        [HttpGet("GetCoursesBySpecialty")]
        public async Task<IActionResult> GetActivePrivateCoursesBySpecialty(int specialtyId)
        {
            var courses = await _courseService.GetActiveCoursesBySpecialtyAsync(specialtyId);
            return Json(courses.Select(c => new { c.CourseID, c.Title }));
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("RegisterCourse")]
        public async Task<IActionResult> RegisterCourse(int childId, int courseId, decimal scheduledHours)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var success = await _courseEnrollmentService.AddRegisteredEnrollmentAsync(childId, courseId, scheduledHours, "Registered", user);

                if (!success)
                {
                    TempData["ErrorMessage1"] = "Enrollment failed.";
                }
                else
                {
                    TempData["SuccessMessage1"] = "Child enrolled successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage1"] = $"{ex.Message}";
            }

            return RedirectToAction("ManageRegistrations", new { childId });
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("UnregisterCourse")]
        public async Task<IActionResult> UnregisterCourse(int enrollmentId, int childId)
        {
            try
            {
                var success = await _courseEnrollmentService.RemoveRegisteredEnrollmentAsync(enrollmentId);

                if (!success)
                {
                    TempData["ErrorMessage1"] = "Failed to remove enrollment.";
                }
                else
                {
                    TempData["SuccessMessage1"] = "Enrollment removed successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage1"] = $"{ex.Message}";
            }

            return RedirectToAction("ManageRegistrations", new { childId });
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("RegisterActivity")]
        public async Task<IActionResult> RegisterActivity(int childId, int activityId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var success1 = false;
                var success2 = false;
                success1 = await _activityEnrollmentService.AddRegisteredEnrollmentAsync(childId, activityId, "Registered", user);
                success2 = await _activityEnrollmentService.UpdateActivityStatusToClosedAsync(activityId);
                if (!success1 || !success2)
                {
                    TempData["ErrorMessage2"] = "Failed to enroll in activity.";
                }
                else
                {
                    TempData["SuccessMessage2"] = "Successfully enrolled in activity.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage2"] = $"{ex.Message}";
            }

            return RedirectToAction("ManageRegistrations", new { childId });
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("UnregisterActivity")]
        public async Task<IActionResult> UnregisterActivity(int enrollmentId, int childId)
        {
            try
            {
                var success = await _activityEnrollmentService.RemoveRegisteredEnrollmentAsync(enrollmentId);

                if (!success)
                {
                    TempData["ErrorMessage2"] = "Failed to remove enrollment.";
                }
                else
                {
                    TempData["SuccessMessage2"] = "Enrollment removed successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage2"] = $"{ex.Message}";
            }

            return RedirectToAction("ManageRegistrations", new { childId });
        }


        [Authorize(Roles = "Staff")]
        [HttpGet("ManagePayments/{childId}")]
        public async Task<IActionResult> ManagePayments(int childId)
        {
            // ✅ Pass ChildID to View
            //ViewBag.ChildID = childId;


            var payments = await _paymentService.GetByChildAsync(childId);
            var child = await _childService.GetAsync(childId);
            if (child == null)
            {
                TempData["ErrorMessage"] = "Child not found.";
                return RedirectToAction("List"); // Redirect to child list page if not found
            }
            ManagePaymentsViewModel payment = new ManagePaymentsViewModel
            {
                Payments = payments,
                Child = child
            };

            var parents = await _paymentService.GetParentsByChildAsync(childId);

            // ✅ Populate Parent dropdown
            ViewBag.ParentList = parents.Select(p => new SelectListItem
            {
                Value = p.ParentID.ToString(),
                Text = p.Name
            }).ToList();

            // ✅ Fetch all active payment packages
            var packages = await _paymentService.GetAllActivePackagesAsync();

            // ✅ Populate ViewBag for dropdown
            ViewBag.PaymentPackages = packages.Select(p => new SelectListItem
            {
                Value = p.PackageID.ToString(),
                Text = p.Title
            }).ToList();

            return View("ManagePayments", payment);
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("AddPayment")]

        public async Task<IActionResult> AddPayment(int childId, int parentId, int packageId, decimal amount, DateTime? paymentDate, IFormFile receiptFile)
        {

            try
            {
                string receiptPath = null;

                // ✅ Save the receipt file
                if (receiptFile != null && receiptFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/receipts");
                    Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = $"{Guid.NewGuid()}_{receiptFile.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await receiptFile.CopyToAsync(fileStream);
                    }

                    receiptPath = $"/receipts/{uniqueFileName}";
                }

                Core.Models.User user = await _userManager.GetUserAsync(User);
                var paymentId = await _paymentService.AddAndReturnIdAsync(childId, parentId, packageId, amount, paymentDate, receiptPath, user);
                var result = await _balanceService.AddPaymentToBalanceAsync(childId, paymentId, amount, user.Id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Payment info has been added successfully.";
                    //return RedirectToAction("ManagePayments");
                    return RedirectToAction("ManagePayments", new { childId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment info was not added.";
                    return RedirectToAction("ManagePayments", new { childId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("ManagePayments", new { childId });
            }
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("RemovePayment")]
        public async Task<IActionResult> RemovePayment(int paymentID, int childId)
        {
            try
            {
                var result = await _paymentService.RemoveAsync(paymentID);
                if (result)
                {
                    TempData["SuccessMessage"] = "Payment info has been deleted.";
                    return RedirectToAction("ManagePayments", new { childId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment info is not deleted.";
                    return RedirectToAction("ManagePayments", new { childId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("ManagePayments", new { childId });
            }


        }

        [Authorize(Roles = "Staff")]
        [HttpGet("EnrollmentsHistory/{childId}")]

        public async Task<IActionResult> EnrollmentsHistory(int childId)
        {

            var child = await _childService.GetAsync(childId);
            var completedCourses = await _courseEnrollmentService.GetCompletedEnrollmentsByChildAsync(childId);
            var completedActivities = await _activityEnrollmentService.GetCompletedEnrollmentsByChildAsync(childId);

            if (child == null)
                throw new Exception("The child can't be found");

            EnrollmentsHistoryViewModel enrollmentHistory = new EnrollmentsHistoryViewModel
            {
                Child = child,
                CompletedCourses = (List<CourseEnrollment>)completedCourses,
                CompletedActivities = (List<ActivityEnrollment>)completedActivities
            };

            return View("EnrollmentsHistory", enrollmentHistory);
        }



        [Authorize(Roles = "Child")]
        [HttpGet("MySchedules")]
        public async Task<IActionResult> MySchedules()
        {
            // Get the currently logged-in user
            Core.Models.User user = await _userManager.GetUserAsync(User);
            var child = await _childService.GetByIdAsync(user.Id);


            //var scheduledCourses = await _courseEnrollmentService.GetScheduledSessionsByChildAsync(child.ChildID);
            var scheduledCourses = await _courseEnrollmentService.GetUpcomingEnrollmentsByChildAsync(child.ChildID);

            var courseSchedulesList = scheduledCourses
                .GroupBy(e => e.Course)
                .Select(group => new CourseSchedulesViewModel
                {
                    Course = group.Key,
                    CourseID = group.Key.CourseID,
                    Schedules = group.ToList()
                }).ToList();


            var scheduledCoursesToConfirm = await _courseEnrollmentService.GetScheduledSessionsToConfirmByChildAsync(child.ChildID);

            var courseSchedulesToConfirmList = scheduledCoursesToConfirm
                .GroupBy(e => e.Course)
                .Select(group => new CourseSchedulesViewModel
                {
                    Course = group.Key,
                    CourseID = group.Key.CourseID,
                    Schedules = group.ToList()
                }).ToList();



            var viewModel = new ChildSchedulesViewModel
            {
                Child = child,
                ChildID = child.ChildID,
                CoursesSchedules = courseSchedulesList,
                CoursesSchedulesToConfirm = courseSchedulesToConfirmList
            };

            return View("MySchedules", viewModel);
        }





        [Authorize(Roles = "Child")]
        [HttpGet("MyEnrollmentsHistory")]
        public async Task<IActionResult> MyEnrollmentsHistory()
        {
            Core.Models.User user = await _userManager.GetUserAsync(User);
            var child = await _childService.GetByIdAsync(user.Id);

            
            var completedCourses = await _courseEnrollmentService.GetCompletedEnrollmentsByChildAsync(child.ChildID);
            var completedActivities = await _activityEnrollmentService.GetCompletedEnrollmentsByChildAsync(child.ChildID);


            EnrollmentsHistoryViewModel enrollmentHistory = new EnrollmentsHistoryViewModel
            {
                Child = child,
                CompletedCourses = (List<CourseEnrollment>)completedCourses,
                CompletedActivities = (List<ActivityEnrollment>)completedActivities
            };

            return View("MyEnrollmentsHistory", enrollmentHistory);
        }



        [Authorize(Roles = "Child")]
        [HttpGet("MyBalance")]
        public async Task<IActionResult> MyBalance()
        {
            Core.Models.User user = await _userManager.GetUserAsync(User);
            var child = await _childService.GetByIdAsync(user.Id);
            var balances = await _balanceService.GetBalanceHistoryAsync(child.ChildID);
            var finalBalance = await _balanceService.GetFinalBalanceAsync(child.ChildID);
            ViewBag.FinalBalance = finalBalance;
            return View(balances);
        }


        //Add course sessions to a child who has registered to a group course 
        [Authorize(Roles = "Staff")]
        [HttpGet("ManageSessionRegistrations")]
        public async Task<IActionResult> ManageSessionRegistrations(int childId, int courseId)
        {

            ViewBag.ChildID = childId;
            ViewBag.CourseID = courseId;

            var sessionOptions = new List<SessionOption>();

            // Sessions available to register
            var sessions = await _courseEnrollmentService.GetOpenSessionsByCourseAsync(courseId);

            // Sessions the child already registered to
            //var registeredSessions = await _courseEnrollmentService.GetRegisteredByCourseChildAsync(courseId, childId);
            var allEnrolledSessions = await _courseEnrollmentService.GetEnrollmentsByCourseChildAsync(courseId, childId);
            if (sessions != null)
            {
                foreach (var session in sessions) {

                    if (allEnrolledSessions.Any(e => e.ScheduledAt == session.ScheduledAt))
                    {
                        continue;
                    }

                    var sessionOption = new ManageSessionRegistrationsViewModel.SessionOption
                    { EnrollmentID = session.EnrollmentID,
                        ScheduledAt = session.ScheduledAt ?? DateTime.MinValue,
                        ScheduledHours = session.ScheduledHours ?? 0,
                        IsSelected = false
                    };
                    sessionOptions.Add(sessionOption);

                }
            }


            //var allSessions = await _courseEnrollmentService.GetEnrollmentsByCourseChildAsync(courseId, childId);

            var all_sessions = allEnrolledSessions.Select(e => new SessionViewModel
            {
                EnrollmentID = e.EnrollmentID,
                ScheduledAt = e.ScheduledAt ?? DateTime.MinValue,
                ScheduledHours = e.ScheduledHours ?? 0,
                Status = e.Status,
                ParentNote = e.ParentNote,
                StaffNote = e.StaffNote
            }).ToList();

            //var scheduledSessions = await _courseEnrollmentService.GetSchedulesByCourseChildAsync(courseId, childId);

            //var scheduled_sessions = scheduledSessions.Select(e => new SessionViewModel
            //{
            //    EnrollmentID = e.EnrollmentID,
            //    ScheduledAt = e.ScheduledAt ?? DateTime.MinValue,
            //    ScheduledHours = e.ScheduledHours ?? 0,
            //    Status = e.Status,
            //    ParentNote = e.ParentNote,
            //    StaffNote = e.StaffNote
            //}).ToList();

            Child child = await _childService.GetAsync(childId);
            Course course = await _courseService.GetAsync(courseId);

            var viewModel = new ManageSessionRegistrationsViewModel
            {
                Child = child,
                Course = course,
                ChildID = childId,
                CourseID = courseId,
                AvailableSessions = sessionOptions,
                AllSessions = all_sessions,
                //ScheduledSessions = scheduled_sessions,
                CourseSessionsCount = (int)course.SessionCount,
                EnrolledSessionsCount = allEnrolledSessions.Count()

            };

            return View(viewModel);
        }

        //Add course sessions to a child who has registered to a group course 
        [Authorize(Roles = "Staff")]
        [HttpPost("AddRegisteredSessions")]
        public async Task<IActionResult> AddRegisteredSessions(ManageSessionRegistrationsViewModel model)
        {
            try
            {
                Core.Models.User user = await _userManager.GetUserAsync(User);
                var selectedSessions = model.AvailableSessions.Where(s => s.IsSelected).ToList();
                

                if (selectedSessions.Count + model.EnrolledSessionsCount > model.CourseSessionsCount)
                {
                    TempData["ErrorMessage"] = "You can't register more than " + model.CourseSessionsCount + " sessions";
                    return RedirectToAction("ManageSessionRegistrations", new { childId = model.ChildID, courseId = model.CourseID });

                }
                    

                foreach (var session in selectedSessions)
                {

                    var sessionRef = await _courseEnrollmentService.GetAsync(session.EnrollmentID);

                    if (sessionRef == null) continue;



                    var success = await _courseEnrollmentService.AddSessionRegisteredEnrollmentAsync(model.ChildID, model.CourseID, sessionRef.ScheduledAt, sessionRef.ScheduledHours, sessionRef.EnrollmentID, "Registered", user);

                    if (!success)
                    {
                        TempData["ErrorMessage"] = "Failed to add session.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Session added successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("ManageSessionRegistrations", new { childId = model.ChildID, courseId = model.CourseID });
            }

            return RedirectToAction("ManageSessionRegistrations", new { childId = model.ChildID, courseId = model.CourseID });
        }


        [Authorize(Roles = "Staff")]
        [HttpPost("UpdateAllSessions")]
        public async Task<IActionResult> UpdateAllSessions(UpdateAllSessionsFormModel formModel)
        {
            if (formModel.AllSessions == null || !formModel.AllSessions.Any())
            {
                TempData["ErrorMessage"] = "No sessions submitted.";
                return RedirectToAction("ManageSessionRegistrations", new { courseId = formModel.CourseID, childId = formModel.ChildID });
            }

            foreach (var session in formModel.AllSessions)
            {
                // Example pseudo-code for updating session in database
                //var existingSession = _dbContext.CourseEnrollments.FirstOrDefault(e => e.EnrollmentID == session.EnrollmentID);
                var existingSession = await _courseEnrollmentService.GetAsync(session.EnrollmentID);
                if (existingSession != null)
                {
                    existingSession.Status = session.Status;
                    existingSession.StaffNote = session.StaffNote;
                    var result = await _courseEnrollmentService.UpdateSessionAsync(existingSession);
                }
                
            }

            
            //_dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Session updates saved successfully.";
            return RedirectToAction("ManageSessionRegistrations", new { childId = formModel.ChildID, courseId = formModel.CourseID});

            //try
            //{
            //    foreach (var session in model.AllSessions)
            //    {
            //        var enrollment = await _courseEnrollmentService.GetAsync(session.EnrollmentID);

            //        if (enrollment != null)
            //        {
            //            enrollment.Status = session.Status;
            //            enrollment.StaffNote = session.StaffNote;
            //            enrollment.UpdatedDate = DateTime.UtcNow;
            //            var result = await _courseEnrollmentService.UpdateSessionAsync(enrollment);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    TempData["ErrorMessage"] = $"{ex.Message}";
            //    return RedirectToAction("ManageSessionRegistrations", new { childId = model.ChildID, courseId = model.CourseID });
            //}

            //return RedirectToAction("ManageSessionRegistrations", new { childId = model.ChildID, courseId = model.CourseID });
        }



        [Authorize(Roles = "Child")]
        [HttpPost("UpdateSchedules")]
        public async Task<IActionResult> UpdateSchedules(UpdateSchedulesFormModel model)
        {
            if (model?.Schedules != null && model.Schedules.Any())
            {
                try
                {
                    

                    foreach (var schedule in model.Schedules)
                    {
                        var existing = await _courseEnrollmentService.GetAsync(schedule.EnrollmentID);
                        if (existing != null)
                        {
                            if (existing.Status != "Canceled" && existing.Status != "Deleted" && schedule.Status != null)  //schedule.Status is null means Status dropdown list is enabled
                            {
                                existing.Status = schedule.Status;
                                existing.ParentNote = schedule.ParentNote;
                            }

                            await _courseEnrollmentService.UpdateSessionAsync(existing);
                        }
                    }

                    TempData["SuccessMessage1"] = "Schedules updated successfully.";
                    TempData["CourseID"] = model.CourseID;
                }

                catch (Exception ex)
                {
                    TempData["ErrorMessage1"] = ex.Message;   //This seems to be strange
                    TempData["CourseID"] = model.CourseID;
                }

            }
           

            return RedirectToAction("MySchedules");
        }





        [Authorize(Roles = "Child")]
        [HttpPost("UpdateSchedulesToConform")]
       
         public async Task<IActionResult> UpdateSchedulesToConform(UpdateSchedulesToConfirmFormModel model, string actionType)
        {
            if(actionType == "SaveChanges")
            {
                if (model?.Schedules != null && model.Schedules.Any())
                {
                    foreach (var schedule in model.Schedules)
                    {
                        var existing = await _courseEnrollmentService.GetAsync(schedule.EnrollmentID);
                        if (existing != null)
                        {
                            if (existing.Status != "Deleted")
                            {
                                existing.ParentNote = schedule.ParentNote;
                            }

                            await _courseEnrollmentService.UpdateSessionAsync(existing);
                        }
                    }

                    TempData["SuccessMessage2"] = "Schedules updated successfully.";
                }
                //else
                //{
                //    TempData["ErrorMessage2"] = "No schedules to update.";   //This seems to be strange
                //}
            }

            else if (actionType == "Confirm")
            {
                // Handle Confirm logic
                if (model?.Schedules != null && model.Schedules.Any())
                {
                    foreach (var schedule in model.Schedules)
                    {
                        var existing = await _courseEnrollmentService.GetAsync(schedule.EnrollmentID);
                        if (existing != null)
                        {
                            if (existing.Status == "Registered")
                            {
                                existing.Status = "Scheduled";
                            }

                            await _courseEnrollmentService.UpdateSessionAsync(existing);
                        }
                    }

                    TempData["SuccessMessage2"] = "Course schedules confirmed successfully.";
                }
            }


            return RedirectToAction("MySchedules");
        }



        //[Authorize(Roles = "Staff")]
        //[HttpPost]
        //public async Task<IActionResult> UpdateScheduledSessions(ManageSessionRegistrationsViewModel model)
        //{
        //    try
        //    {
        //        foreach (var session in model.AllSessions)
        //        {
        //            var enrollment = await _courseEnrollmentService.GetAsync(session.EnrollmentID);

        //            if (enrollment != null)
        //            {
        //                enrollment.Status = session.Status;
        //                enrollment.StaffNote = session.StaffNote;
        //                enrollment.UpdatedDate = DateTime.UtcNow;
        //                var result = await _courseEnrollmentService.UpdateSessionAsync(enrollment);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage2"] = $"{ex.Message}";
        //        return RedirectToAction("ManageSessionRegistrations", new { childId = model.ChildID, courseId = model.CourseID });
        //    }

        //    return RedirectToAction("ManageSessionRegistrations", new { childId = model.ChildID, courseId = model.CourseID });
        //}


    }
}




