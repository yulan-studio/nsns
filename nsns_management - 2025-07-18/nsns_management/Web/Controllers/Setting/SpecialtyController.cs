
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using Core.Models;
using Core.Contexts;
using System.Diagnostics;
using Core.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis;
using Core.ViewModels;



namespace Web.Controllers.Setting
{
    [Route("Specialty")]
    public class SpecialtyController : Controller
    {
        //private readonly AppDbContext _context;
        private ISpecialtyService _specialtyService;
        private ICoachSpecialtyService _coachSpecialtyService;
        private readonly UserManager<Core.Models.User> _userManager;
        public SpecialtyController(ISpecialtyService specialtyService, ICoachSpecialtyService coachSpecialtyService, UserManager<Core.Models.User> userManager)
        {
            _specialtyService = specialtyService;
            _coachSpecialtyService = coachSpecialtyService;
            _userManager = userManager;
        }

        // ✅ Load City List
        [HttpGet("List")]
        public async Task<IActionResult> List()
        {
            //var cities = await _context.Cities.ToListAsync();
            var specialties = await _specialtyService.GetAllAsync();

            List<SpecialtyWithDeleteViewModel> specialtiesWithDelete = new List<SpecialtyWithDeleteViewModel>();

            foreach (Specialty specialty in specialties)
            {
                SpecialtyWithDeleteViewModel specialtyWithDelete = new SpecialtyWithDeleteViewModel();
                specialtyWithDelete.Specialty = specialty;
                bool canDelete = !(await _coachSpecialtyService.GetCoachesBySpecialtyAsync(specialty.SpecialtyID)).Any();
                specialtyWithDelete.CanDelete = canDelete;
                specialtiesWithDelete.Add(specialtyWithDelete);
            }
            return View(specialtiesWithDelete); // Ensure there is a corresponding List.cshtml in Views/Staff

        }

        // ✅ Load Partial View for Add/Edit Form
        [HttpGet("Add")]
        public async Task<IActionResult> Add()
        {

            return PartialView("_Add", new Specialty { Title = string.Empty, Description = string.Empty });
        }


        [HttpGet("Edit/{specialtyId}")]
        public async Task<IActionResult> Edit(int specialtyId)
        {


            //var city = await _context.Cities.FindAsync(cityId);
            var specialty = await _specialtyService.GetAsync(specialtyId);
            if (specialty == null) return NotFound();

            return PartialView("_Edit", specialty);
        }





        // ✅ Save Specialty (Add / Edit)
        [HttpPost("Save")]
        public async Task<IActionResult> Save(Specialty specialty)
        {

            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var user = await _userManager.GetUserAsync(User);
             
            try
            {
                var result = false;
                if (specialty.SpecialtyID == 0)
                {
                    specialty.CreatedBy = user.Id;
                    result = await _specialtyService.AddAsync(specialty);
                }
                else
                {
                    specialty.UpdatedBy = user.Id;
                    result = await _specialtyService.UpdateAsync(specialty);
                }

                if (result)
                    return Json(new { success = true });
                else
                    return Json(new { success = false });


            }

            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }); // ✅ Return error message
            }


        }


        // ✅ Load Partial View for Add/Edit Form
        [HttpGet("DeleteConfirm/{specialtyId}")]
        public async Task<IActionResult> DeleteConfirm(int specialtyId)
        {
           
            var specialty = await _specialtyService.GetAsync(specialtyId);
            if (specialty == null) return NotFound();

            return PartialView("_DeleteConfirm", specialty);
        }

        // ✅ Delete Specialty
        [HttpPost("Delete/{specialtyId}")]
        public async Task<IActionResult> Delete(int specialtyId)
        {
            //var city = await _context.Cities.FindAsync(cityId);
            var result = await _specialtyService.DeleteAsync(specialtyId);
            //if (city == null) return NotFound();

            //_context.Cities.Remove(city);
            //await _context.SaveChangesAsync();
            if (result)
                return Json(new { success = true });
            else
                return Json(new { success = false });

        }
    }

}
