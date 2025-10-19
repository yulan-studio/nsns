
using Core.Interfaces;
using Core.Models;
using Core.Repositories;
using Core.Services;
using Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;
using System.Data.SqlClient;



namespace Web.Controllers.User
{
    [Route("Coach")]
    //[ApiController]
    public class CoachController : Controller
    {
        private readonly ICoachService _coachService;
        private readonly ICoachIncomeService _incomeService;
        private readonly IChildBalanceService _balanceService;
        private readonly ICoachRepository _coachRepository;
        private readonly ICityService _cityService;
        private readonly ISpecialtyService _specialtyService;

        private readonly ICoachSpecialtyService _coachSpecialtyService;
        private readonly ICourseEnrollmentService _courseEnrollmentService;
        private readonly ICourseService _courseService;
        private readonly IChildService _childService;
        private readonly IParentChildService _parentChildService;
        private readonly IFeeService _feeService;

        private readonly EmailService _emailService;
        private readonly UserManager<Core.Models.User> _userManager;
        
        public CoachController(ICoachService coachService, ICoachRepository coachRepository, ICoachIncomeService incomeService, IChildBalanceService balanceService, ICityService cityService, ISpecialtyService specialtyService, ICoachSpecialtyService coachSpecialtyService, ICourseEnrollmentService courseEnrollmentService, ICourseService courseService, IChildService childService, IParentChildService parentChildService, IFeeService feeService, EmailService emailService, UserManager<Core.Models.User> userManager)
        {
            _coachService = coachService;
            _incomeService = incomeService;
            _balanceService = balanceService;
            _coachRepository = coachRepository;
            _cityService = cityService;
            _specialtyService = specialtyService;
            _coachSpecialtyService = coachSpecialtyService;
            _courseEnrollmentService = courseEnrollmentService;
            _courseService = courseService;
            _childService = childService;
            _parentChildService = parentChildService;
            _feeService = feeService;
            _emailService = emailService;
            _userManager = userManager;
            
        }

        [Authorize(Roles = "Staff")]
        // POST: Add Staff Action
        [HttpPost("Add")]
        //[HttpPost]
        public async Task<IActionResult> Add(string name, string email, string password, List<int> specialtyIds, string gender, string phone, string wechat, int cityId)
        {

           
            if (!ModelState.IsValid)
            {
                var cities = await _cityService.GetAllAsync(); // Replace with your data fetching logic
                ViewBag.CityList = cities.Select(c => new SelectListItem
                {
                    Value = c.CityID.ToString(),
                    Text = c.Name
                }).ToList();

                var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic
                ViewBag.SpecialtyList = specialties.Select(c => new SelectListItem
                {
                    Value = c.SpecialtyID.ToString(),
                    Text = c.Title
                }).ToList();
                return View();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                var result = await _coachService.AddAsync(name, email, password, specialtyIds, gender, phone, wechat, cityId, user);
                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Failed in adding the coach info.");

                   
                    // Repopulate CityList for the dropdown if validation fails

                    var cities = await _cityService.GetAllAsync(); // Replace with your data fetching logic
                    ViewBag.CityList = cities.Select(c => new SelectListItem
                    {
                        Value = c.CityID.ToString(),
                        Text = c.Name
                    }).ToList();

                    var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic
                    ViewBag.SpecialtyList = specialties.Select(c => new SelectListItem
                    {
                        Value = c.SpecialtyID.ToString(),
                        Text = c.Title
                    }).ToList();


                    return View();
                }
                TempData["SuccessMessage"] = "Coach info has been added successfully.";
                return RedirectToAction("List"); // Redirect to the coach list page


            }
            catch (Exception ex)
            {
                ModelState.AddModelError(String.Empty, $"{ex.Message}");
                
                // Repopulate CityList for the dropdown if validation fails

                var cities = await _cityService.GetAllAsync(); // Replace with your data fetching logic
                ViewBag.CityList = cities.Select(c => new SelectListItem
                {
                    Value = c.CityID.ToString(),
                    Text = c.Name
                }).ToList();

                var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic
                ViewBag.SpecialtyList = specialties.Select(c => new SelectListItem
                {
                    Value = c.SpecialtyID.ToString(),
                    Text = c.Title
                }).ToList();

                  
                return View();
            }

            


        }

        [Authorize(Roles = "Staff")]
        // GET: Add View
        [HttpGet("Add")]
        //[HttpGet]
        public async Task<IActionResult> AddAsync()
        {
            var cities = await _cityService.GetAllAsync(); // Replace with your data fetching logic
            ViewBag.CityList = cities.Select(c => new SelectListItem
            {
                Value = c.CityID.ToString(),
                Text = c.Name
            }).ToList();

            var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic
            ViewBag.SpecialtyList = specialties.Select(c => new SelectListItem
            {
                Value = c.SpecialtyID.ToString(),
                Text = c.Title
            }).ToList();


            return View();

        }




        [Authorize(Roles = "Staff")]
        // GET: Coach/Delete/{userId}
        [HttpGet("ConfirmDelete/{coachId}")]
        public async Task<IActionResult> ConfirmDelete(int coachId)
        {
            // Fetch the staff details from the database
            var coach = await _coachService.GetAsync(coachId);
            if (coach == null)
            {
                return NotFound();
            }

            // Pass the staff details to the Delete.cshtml view
            return View(coach);
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int coachId)
        {
            try
            {
                var result = await _coachService.RemoveAsync(coachId);

                if (!result)
                {
                    TempData["ErrorMessage"] = "The coach member could not be deleted.";
                    return RedirectToAction("List");
                }

                TempData["SuccessMessage"] = "Coach member has been deleted successfully.";
                return RedirectToAction("List"); // Redirect to the coach list page
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("List");
            }
        }





        // GET: Add View
        [HttpGet("List")]
        //[HttpGet]
        public async Task<IActionResult> List(string sortOrder)
        {
            ViewData["NameSortParm"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["GenderSortParm"] = sortOrder == "gender" ? "gender_desc" : "gender";
            ViewData["CitySortParm"] = sortOrder == "city" ? "city_desc" : "city";
            ViewData["CurrentSort"] = sortOrder;

            var coachList = await _coachService.GetAllAsync();

            coachList = sortOrder switch
            {
                "name" => coachList.OrderBy(c => c.Name),
                "name_desc" => coachList.OrderByDescending(c => c.Name),
                "gender" => coachList.OrderBy(c => c.Gender),
                "gender_desc" => coachList.OrderByDescending(c => c.Gender),
                "city" => coachList.OrderBy(c => c.City.Name),
                "city_desc" => coachList.OrderByDescending(c => c.City.Name),
                
                _ => coachList.OrderBy(c => c.Name) // default
            };


            List<CoachWithDeleteViewModel> coaches = new List<CoachWithDeleteViewModel>();

            foreach (Coach coach in coachList)
            {
                CoachWithDeleteViewModel coachWithDelete = new CoachWithDeleteViewModel();
                coachWithDelete.Coach = coach;
                bool canDelete =!(await _courseService.GetCoursesByCoachAsync(coach.CoachID)).Any();
                coachWithDelete.CanDelete = canDelete;
                coaches.Add(coachWithDelete);
            }
            return View(coaches); // Ensure there is a corresponding List.cshtml in Views/Staff

           
        }


        [Authorize(Roles = "Staff, Coach")]
        // GET: Edit View
        [HttpGet("Edit/{coachId}")]
        //[HttpGet]
        public async Task<IActionResult> Edit(int coachId)
        {
            //Fetch the staff details from the database


           var coach = await _coachService.GetAsync(coachId);

            if (coach == null)
            {
                return NotFound();
            }

            var cities = await _cityService.GetAllAsync(); // Replace with your data fetching logic



            ViewBag.CityList = cities.Select(c => new SelectListItem
            {
                Value = c.CityID.ToString(),
                Text = c.Name,
                Selected = c.CityID == coach.CityID
            }).ToList();


            var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic

            //var coachSpecialtyIds = (await _coachSpecialtyService.GetSpecialtyIdsByCoachAsync(coachId)).ToHashSet(); // Get coach's specialties
            var coachSpecialtyIds = coach.CoachSpecialties?.Select(cs => cs.SpecialtyID).ToHashSet() ?? new HashSet<int>();


            ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
            {
                Value = s.SpecialtyID.ToString(),
                Text = s.Title,
                Selected = coachSpecialtyIds.Contains(s.SpecialtyID)
            }).ToList();

            //ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
            //{
            //    Value = s.SpecialtyID.ToString(),
            //    Text = s.Title,
            //    Selected = s.SpecialtyID == coach.SpecialtyID
            //}).ToList();

            // Pass the coach details to the Edit.cshtml view
            return View(coach);
            

        }

        [Authorize(Roles = "Staff, Coach")]
        [HttpPost("Edit/{coachId}")]
        [ValidateAntiForgeryToken]

       
        public async Task<IActionResult> Edit(int coachId, string name, string email, /*string password,*/List<int> specialtyIds, string gender, string phone, string wechat, int cityId)
        {
           

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _coachService.UpdateAsync(coachId, name, email, /*password,*/specialtyIds, gender, phone, wechat, cityId, user);


                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Failed to update coach information.");
                    var coach = await _coachService.GetAsync(coachId);

                    if (coach == null)
                    {
                        return NotFound();
                    }

                    var cities = await _cityService.GetAllAsync(); // Replace with your data fetching logic



                    ViewBag.CityList = cities.Select(c => new SelectListItem
                    {
                        Value = c.CityID.ToString(),
                        Text = c.Name,
                        Selected = c.CityID == coach.CityID
                    }).ToList();


                    //var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic

                    //ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
                    //{
                    //    Value = s.SpecialtyID.ToString(),
                    //    Text = s.Title,
                    //    Selected = s.SpecialtyID == coach.SpecialtyID
                    //}).ToList();

                    var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic

                    //var coachSpecialtyIds = (await _coachSpecialtyService.GetSpecialtyIdsByCoachAsync(coachId)).ToHashSet(); // Get coach's specialties
                    var coachSpecialtyIds = coach.CoachSpecialties?.Select(cs => cs.SpecialtyID).ToHashSet() ?? new HashSet<int>();

                    ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
                    {
                        Value = s.SpecialtyID.ToString(),
                        Text = s.Title,
                        Selected = coachSpecialtyIds.Contains(s.SpecialtyID)
                    }).ToList();


                    // Pass the coach details to the Edit.cshtml view
                    return View(coach);
                }

                TempData["SuccessMessage"] = "coach information updated successfully.";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                //TempData["ErrorMessage"] = $"Error: {ex.Message}";
                var coach = await _coachService.GetAsync(coachId);

                if (coach == null)
                {
                    return NotFound();
                }

                var cities = await _cityService.GetAllAsync(); // Replace with your data fetching logic



                ViewBag.CityList = cities.Select(c => new SelectListItem
                {
                    Value = c.CityID.ToString(),
                    Text = c.Name,
                    Selected = c.CityID == coach.CityID
                }).ToList();


                //var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic

                //ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
                //{
                //    Value = s.SpecialtyID.ToString(),
                //    Text = s.Title,
                //    Selected = s.SpecialtyID == coach.SpecialtyID
                //}).ToList();

                var specialties = await _specialtyService.GetAllAsync(); // Replace with your data fetching logic

                var coachSpecialtyIds = coach.CoachSpecialties?.Select(cs => cs.SpecialtyID).ToHashSet() ?? new HashSet<int>();

                ViewBag.SpecialtyList = specialties.Select(s => new SelectListItem
                {
                    Value = s.SpecialtyID.ToString(),
                    Text = s.Title,
                    Selected = coachSpecialtyIds.Contains(s.SpecialtyID)
                }).ToList();


                // Pass the coach details to the Edit.cshtml view
                return View(coach);
            }
        }

        [Authorize(Roles = "Coach")]
        [HttpGet("ManageCourse")]
        public async Task<IActionResult> ManageCourse()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var coach = await _coachRepository.GetCoachByIdAsync(user.Id);
                int coachId = coach.CoachID;

                var model = new ManageCourseViewModel();
                model.Coach = coach;


                var specialties = await _coachSpecialtyService.GetSpecialtiesByCoachAsync(coachId);

                if (specialties == null || !specialties.Any())
                {
                    TempData["ErrorMessage"] = "No specialties found for this coach.";
                    return RedirectToAction("Index", "Home"); // Redirect to a safe page
                }

                var specialtiesCourses = new List<SpecialtyCoursesViewModel>();

                foreach (Specialty specialty in specialties)
                {
                    var specialtyCourses = new SpecialtyCoursesViewModel();
                    specialtyCourses.SpecialtyID = specialty.SpecialtyID;
                    specialtyCourses.SpecialtyTitle = specialty.Title;


                    var courses = await _courseService.GetActiveCourseByCoachBySpecialtyAsync(coachId, specialty.SpecialtyID);
                    //if (courses == null || !courses.Any())
                    //{
                    //    continue; // Skip if no courses
                    //}

                    var coursesChildren = new List<CourseChildrenViewModel>();


                    foreach (Course course in courses)
                    {
                        var courseChildren = new CourseChildrenViewModel();
                        courseChildren.CourseID = course.CourseID;
                        courseChildren.CourseTitle = course.Title;
                        courseChildren.SessionCount = course.SessionCount;



                        var children = (List<ChildViewModel>)await _courseEnrollmentService.GetRegisterationByCourseAsync(course.CourseID);
                        courseChildren.RegisteredChildren = children;



                        coursesChildren.Add(courseChildren);

                    }

                    specialtyCourses.Courses = coursesChildren;

                    specialtiesCourses.Add(specialtyCourses);

                }

                model.Specialties = specialtiesCourses;

                return View(model);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", "Home"); // Redirect to a safe page
            }


        }


        [Authorize(Roles = "Coach")]
        [HttpGet("ManageSchedules/{childId}")]
        public async Task<IActionResult> ManageSchedules(int childId, [FromQuery] int courseId, [FromQuery] int enrollmentId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var coach = await _coachRepository.GetCoachByIdAsync(user.Id);
                int coachId = coach.CoachID;
                // ✅ Get children who are enrolled in the coach's courses
                var child = await _childService.GetAsync(childId);
                var parents = await _parentChildService.GetParentsByChildIdAsync(childId);

                // ✅ Get courses assigned to the coach
                //var course = await _courseService.GetActiveCourseByCoachAsync(coachId);
                //int courseId = 2; //need to change later
                var course = await _courseService.GetAsync(courseId);

                // ✅ Get schedules for the child and course
                List<CourseEnrollment> schedules = (List<CourseEnrollment>)await _courseEnrollmentService.GetUpcomingEnrollmentsByCourseChildAsync(course.CourseID, childId);

                List<CourseEnrollment> completed = (List<CourseEnrollment>)await _courseEnrollmentService.GetCompletesByCourseChildAsync(courseId, childId);

                List<CourseEnrollment> scheduled = (List<CourseEnrollment>)await _courseEnrollmentService.GetSchedulesByCourseChildAsync(courseId, childId);

               var model = new ManageSchedulesViewModel
                {
                    EnrollmentID = enrollmentId,
                    Child = child,
                    Parents = parents,
                    Course = course,
                    Schedules = schedules,
                    ScheduledCount = scheduled.Count,
                    CompletedCount = completed.Count
                };

                return View(model);
            }

            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"{ex.Message}";
                return RedirectToAction("ManageCourse");
            }
        }

        [Authorize(Roles = "Coach")]
        [HttpPost("ScheduleCourse")]
        public async Task<IActionResult> ScheduleCourse(int childId, int courseId, DateTime scheduledAt, decimal scheduledHours, string location, int enrollmentId_Ref)
        {
            var user = await _userManager.GetUserAsync(User);
            var coach = await _coachRepository.GetCoachByIdAsync(user.Id);
            int coachId = coach.CoachID;


            Child? child = await _childService.GetAsync(childId);
            if (child == null)
            {
                throw new ArgumentException("Child not found");
            }
            var torontoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var nowToronto = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, torontoTimeZone);

            if (scheduledAt < nowToronto)
            {
                TempData["ErrorMessage"] = "Please choose a future time.";
            }

            else
            { 
                var course = await _courseService.GetAsync(courseId); // Ensure the course exists
                if (course.SessionCount != null)
                {
                    var scheduledCount = (await _courseEnrollmentService.GetSchedulesByCourseChildAsync(courseId, childId)).Count();
                    var completedCount = (await _courseEnrollmentService.GetCompletesByCourseChildAsync(courseId, childId)).Count();
                    // Check if the maximum number of sessions has been reached
                    if (scheduledCount + completedCount >= course.SessionCount)
                    {
                        TempData["ErrorMessage"] = "The maximum number of sessions for this course has been reached.";
                        return RedirectToAction("ManageSchedules", new { childId, courseId = courseId, enrollmentId = enrollmentId_Ref });
                    }
                }
                bool result = await _courseEnrollmentService.ScheduleCourseAsync(childId, courseId, scheduledAt, scheduledHours, location, coachId, enrollmentId_Ref);

                if (result)
                {


                    var subject = "A Course schedule has been added";

                    var message = "A course schedule has been added for the child: " + child.Name + ":\n" +
                        "Course: " + course.Title + "\n" +
                        "Coach: " + coach.Name + "\n" +
                        "Scheduled At: " + scheduledAt.ToString("yyyy - MM - dd HH: mm") + "\n" +
                        "Scheduled Hours: " + scheduledHours;

                    await _emailService.SendEmailAsync(child.User.Email, subject, message);  //send to child, how about send to coach?

                    TempData["SuccessMessage"] = "Course scheduled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to schedule the course.";
                }
            }

            return RedirectToAction("ManageSchedules", new { childId, courseId = courseId, enrollmentId = enrollmentId_Ref });
        }


        [Authorize(Roles = "Coach")]
        [HttpPost("DeleteSchedule")]
        public async Task<IActionResult> DeleteSchedule(int enrollmentId, int childId, int courseId, string coachNote, int enrollmentId_Ref)
        {

            var child = await _childService.GetAsync(childId);
            var course = await _courseService.GetAsync(courseId);
            var user = await _userManager.GetUserAsync(User);
            var coach = await _coachRepository.GetCoachByIdAsync(user.Id);

            var enrollment = await _courseEnrollmentService.GetAsync(enrollmentId);

            bool result = await _courseEnrollmentService.RemoveScheduleAsync(enrollmentId, coachNote);
            

            if (result)
            {
                var subject = "A Course schedule has been deleted";

                var message = "A course schedule has been deleted for the child: " + child.Name + ":\n" +
                    "Course: " + course.Title + "\n" +
                    "Coach: " + coach.Name + "\n" +
                    "Scheduled At: " + enrollment.ScheduledAt?.ToString("yyyy - MM - dd HH: mm") + "\n" +
                    "Scheduled Hours: " + enrollment.ScheduledHours;

                await _emailService.SendEmailAsync(child.User.Email, subject, message);  //send to child

                TempData["SuccessMessage"] = "Schedule deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete the schedule.";
            }

            return RedirectToAction("ManageSchedules", new { childId, courseId = courseId , enrollmentId = enrollmentId_Ref });
        }


        [Authorize(Roles = "Coach")]
        [HttpGet("ManageEnrollments/{childId}")]
        public async Task<IActionResult> ManageEnrollments(int childId, [FromQuery] int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            var coach = await _coachRepository.GetCoachByIdAsync(user.Id);
            int coachId = coach.CoachID;

            // Get all children registered in the coach's course
            //var children = await _courseEnrollmentService.GetRegisterationByCoachAsync(coachId);

            // Get enrollment details
            //var course = await _courseService.GetActiveCourseByCoachAsync(coachId);
            //int courseId = 2;  //need to change later
            var course = await _courseService.GetAsync(courseId);
            Child? child = await _childService.GetAsync(childId);

            if (child == null)
            {
                throw new ArgumentException("Child not found");
            }

            var model = new ManageEnrollmentsViewModel
            {
                Course = course,
                Child = child,
                //ScheduledEnrollments = (List<CourseEnrollment>)await _courseEnrollmentService.GetSchedulesByCourseChildAsync(course.CourseID, childId),
                
                
                WaitToCompleteEnrollments = (List<CourseEnrollment>)await _courseEnrollmentService.GetWaitToCompleteByCourseChildAsync(course.CourseID, childId),
                CompletedEnrollments = (List<CourseEnrollment>)await _courseEnrollmentService.GetCompletesByCourseChildAsync(course.CourseID, childId)

            };

            return View(model);
        }

        [Authorize(Roles = "Coach")]
        [HttpPost("CompleteSession")]
        public async Task<IActionResult> CompleteSession(int enrollmentId, int childId, int courseId, decimal actualHours)
        {
            //int coachId = 16; // GetLoggedInCoachId(); // Replace with actual logic to get coach ID
            var user = await _userManager.GetUserAsync(User);

            Child? child = await _childService.GetAsync(childId);
            if (child == null)
            {
                throw new ArgumentException("Child not found");
            }

            var course = await _courseService.GetAsync(courseId); // Ensure the course exists
            var courseEnrollment = await _courseEnrollmentService.GetAsync(enrollmentId);

            try
            {
                bool result1 = await _courseEnrollmentService.CompleteSessionAsync(enrollmentId, actualHours);

                //We don't calculate income for Coachs because this will be done by accounting manually
                bool result2 = await _incomeService.UpdateCoachIncomeAsync(enrollmentId, user.Id);
                bool result3 = true;

                Core.Models.Fee? fee = await _feeService.GetFeeForCourseEnrollmentAsync(enrollmentId);
                if(fee!= null && fee.PaymentModel == "Token")
                {
                    result3 = await _balanceService.DeductCourseSessionCostAsync(enrollmentId, user.Id); // Deduct private course cost from child's balance
                }


                //if (result1 && result2 && result3)
                if (result1 && result3)
                    //if (result1)
                    {
                    TempData["SuccessMessage"] = "Course Completed successfully.";


                    var subject = "Course Session Completed successfully.";

                    var message = "The follow course session has been completed for " + child.Name + ":\n" +
                                  "Course: " + course.Title + "\n" +
                                  "Scheduled At: " + (courseEnrollment.ScheduledAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A") + "\n" +
                                  "Actual Hours: " + actualHours;

                    await _emailService.SendEmailAsync(child.User.Email, subject, message);  //send to child



                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to complete the course.";
                }
            }
            catch (Exception ex)
            {
                
                TempData["ErrorMessage"] = $"{ex.Message}";
            }

            return RedirectToAction("ManageEnrollments", new { childId, courseId = courseId});
        }

        [Authorize(Roles = "Coach")]
        [HttpGet("Income")]
        public async Task<IActionResult> Income()
        {
            // Get current coach based on User ID
            var user = await _userManager.GetUserAsync(User);
            var coach = await _coachRepository.GetCoachByIdAsync(user.Id);


            if (coach == null)
                return NotFound("Coach profile not found.");

            // Get income records
            var incomeRecords = await _incomeService.GetCoachIncomeAsync(coach.CoachID);

            var viewModel = incomeRecords.Select(i => new CoachIncomeViewModel
            {
                EnrollmentID = i.EnrollmentID ?? 0,
                CourseName = i.Course?.Title ?? "N/A",
                ChildName = i.Enrollment?.Child.Name ?? "N/A",
                SessionDate = i.Enrollment?.ScheduledAt ?? DateTime.MinValue,
                SessionHours = i.Enrollment?.ActualHours ?? 0,
                IncomeChange = i.IncomeChange ?? 0,
                TotalIncomeSoFar = i.Income ?? 0
            }).ToList();

            ViewBag.TotalIncome = viewModel.LastOrDefault()?.TotalIncomeSoFar ?? 0;

            return View(viewModel);
        }
    }
}

