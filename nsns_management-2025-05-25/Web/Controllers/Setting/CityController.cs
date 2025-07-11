
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
using Core.ViewModels;



namespace Web.Controllers.Setting
{
    [Route("City")]
    public class CityController : Controller
    {
        //private readonly AppDbContext _context;
        private ICityService _cityService;
        private readonly UserManager<Core.Models.User> _userManager;
        public CityController(ICityService cityService, UserManager<Core.Models.User> userManager)
        {
            _cityService = cityService;
            _userManager = userManager;
        }

        // ✅ Load City List
        [HttpGet("List")]
        public async Task<IActionResult> List()
        {
            
            //var cities =await _cityService.GetAllAsync();
            //return View(cities);

            var allCities = await _cityService.GetAllAsync();
            var usedCities = (await _cityService.GetAllUsedAsync()).Select(c => c.CityID).ToHashSet(); // Get used city IDs as HashSet for fast lookup

            var viewModel = new ManageCitiesViewModel
            {
                Cities = allCities,
                UsedCityIds = usedCities
            };

            return View(viewModel);
        }

        

        // ✅ Load Partial View for Add/Edit Form
        [HttpGet("Add")]
        public async Task<IActionResult> Add()
        {
            return PartialView("_Add", new City{ Name = string.Empty });
        }


        [HttpGet("Edit/{cityId}")]
        public async Task<IActionResult> Edit(int cityId)
        {


            //var city = await _context.Cities.FindAsync(cityId);
            var city = await _cityService.GetAsync(cityId);
            if (city == null) return NotFound();

            return PartialView("_Edit", city);
        }





        // ✅ Save City (Add / Edit)
        [HttpPost("Save")]
        public async Task<IActionResult> Save(City city)
        {
            
                if (!ModelState.IsValid)
                    return BadRequest("Invalid data");
            //city.CreatedBy = 1;  //temparaly set it to 1
            var user = await _userManager.GetUserAsync(User);
            try
                {
                   var result = false;
                if (city.CityID == 0)
                {
                    city.CreatedBy = user.Id;
                    city.CreatedDate = DateTime.Now; ;
                    result = await _cityService.AddAsync(city);
                }
                else
                {
                    city.UpdatedBy = user.Id;
                    city.UpdatedDate = DateTime.Now;
                    result = await _cityService.UpdateAsync(city);
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
        [HttpGet("DeleteConfirm/{cityId}")]
        public async Task<IActionResult> DeleteConfirm(int cityId)
        {
            //if (cityId == 0) return PartialView("_DeleteConfirm", new City { Name = string.Empty });

            //var city = await _context.Cities.FindAsync(cityId);
            var city = await _cityService.GetAsync(cityId);
            if (city == null) return NotFound();

            return PartialView("_DeleteConfirm", city);
        }

        // ✅ Delete City
        [HttpPost("Delete/{cityId}")]
        public async Task<IActionResult> Delete(int cityId)
        {
            //var city = await _context.Cities.FindAsync(cityId);
            var result = await _cityService.DeleteAsync(cityId);
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
