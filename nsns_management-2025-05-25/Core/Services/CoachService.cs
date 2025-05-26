using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
//using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Core.Services
{
    

    public class CoachService : ICoachService
    {
        private readonly ICoachRepository _coachRepository;
        private readonly ISpecialtyRepository _specialtyRepository;
        //private readonly ICoachSpecialtyService _coachSpecialtyService;
        private readonly ICityRepository _cityRepository;
        IUserRegistrationService _userRegistrationService;
        private readonly IUserRepository<User> _userRepository;
        private readonly UserManager<Core.Models.User> _userManager;
        //private readonly IPasswordHasher<Coach> _passwordHasher;
        //private readonly JwtOptions _jwtOptions;
        //private const int TokenExpirationMinutes = 60; // Token validity duration



        public CoachService(ICoachRepository coachRepository, ISpecialtyRepository specialtyRepository, ICityRepository cityRepository, IUserRegistrationService userRegistrationService, UserManager<Core.Models.User> userManager, IUserRepository<User> userRepository/*, ICoachSpecialtyService coachSpecialtyService*/ /*, IPasswordHasher<Coach> password, IOptions<JwtOptions> jwtOptions*/)
        {
            _coachRepository = coachRepository;
            _cityRepository = cityRepository;
            _specialtyRepository = specialtyRepository;
            _userRegistrationService = userRegistrationService;
            _userManager = userManager;
            _userRepository = userRepository;
            //_coachSpecialtyService = coachSpecialtyService;
            //_passwordHasher = password;
            //_jwtOptions = jwtOptions.Value;

        }

        public async Task<bool> AddAsync(string name, string email, string password, List<int> specialtyIds, string gender, string phone, string wechat, int cityId, User user)
        {
            // Check if a user with the same email already exists

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                throw new Exception("A user with the same email already exists.");
            }

            // Register the user
            var result = await _userRegistrationService.RegisterUserAsync(email, password, "Coach", user);
            if (!result)
            {
                return false;
            }

            // Retrieve the city entity
            var city = await _cityRepository.GetAsync(cityId);
            if (city == null)
            {
                throw new Exception("No city is added.");
            }

            // Get the newly registered user
            var newUser = await _userManager.FindByEmailAsync(email);
            if (newUser == null)
            {
                return false;
            }

            // Create a new Coach entity
            var coachUser = new Coach
            {
                UserID = newUser.Id,
                Name = name,
                Phone = phone,
                Wechat = wechat,
                CityID = cityId,
                City = city,
                Gender = gender,
                CoachSpecialties = specialtyIds.Select(specialtyId => new CoachSpecialty
                {
                    SpecialtyID = specialtyId
                }).ToList() // Directly associate specialties via the navigation property
            };

            // Add the new coach entity to the repository
            result = await _coachRepository.AddAsync(coachUser);
            return result;

        }



        public async Task<bool> RemoveAsync(int coachId)
        {
            // Find the coach by ID
            var coach = await _coachRepository.GetAsync(coachId);
            if (coach == null)
            {
                throw new Exception("Coach not found.");
            }

            // Remove the coach
            var result = await _coachRepository.RemoveAsync(coach);
            if (result)
                result = await _userRepository.RemoveAsync(coach.User);
            return result;
        }


        public async Task<bool> UpdateAsync(int coachId, string name,string email, /*string password,*/ List<int> specialtyIds, string gender, string phone, string wechat, int cityId, User user)
        {
            // Find the coach by ID
            var coach = await _coachRepository.GetAsync(coachId);
            if (coach == null)
            {
                throw new Exception("Coach not found.");
            }

            // Update fields
            coach.Name = name;
            coach.User.Email = email;
           
            coach.Gender = gender;
            coach.Phone = phone;
            coach.Wechat = wechat;
            coach.CityID = cityId;
            coach.User.UpdatedDate = DateTime.Now;
            coach.User.UpdatedBy = user.Id;
            coach.CoachSpecialties = specialtyIds.Select(specialtyId => new CoachSpecialty
            {
                SpecialtyID = specialtyId
            }).ToList(); // Directly associate specialties via the navigation property


            // Update the password if provided
            //if (!string.IsNullOrWhiteSpace(password))
            //{
            //    coach.Password = _passwordHasher.HashPassword(coach, password);
            //}

            // Save changes
            return await _coachRepository.UpdateAsync(coach);
        }

        public async Task<Coach> GetAsync(int coachId)
        {
            // Retrieve the staff by ID
            var coach = await _coachRepository.GetAsync(coachId);
            //if(coach.CityID!= null)
            //{
            //    var city = await _cityRepository.GetAsync(coach.CityID);
            //    coach.City = city;
            //}
            
                
            

            //Retrieve the Specialty entity

            //if (coach.SpecialtyID != null)
            //{
            //    var specialty = await _specialtyRepository.GetAsync(coach.SpecialtyID);
            //    coach.Specialty = specialty;
            //}

           

            if (coach == null)
            {
                throw new Exception("Coach not found.");
            }

            return coach;
        }




        public async Task<IEnumerable<Coach>> GetAllAsync()
        {
            try
            {
                // Fetch all coach records from the repository
                var coachList = await _coachRepository.GetAllAsync();

                // You can add additional logic or transformations here if necessary
                return coachList;
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed (e.g., logging)
                throw new Exception("An error occurred while retrieving coach records.", ex);
            }
        }


        public async Task<IEnumerable<Coach>> GetCoachesBySpecailtyAsync(int specialtyId)
        {
            try
            {
                // Fetch all staff records from the repository
                var coachList = await _coachRepository.GetCoachesBySpecialtyAsync(specialtyId);

                // You can add additional logic or transformations here if necessary
                return coachList;
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed (e.g., logging)
                throw new Exception("An error occurred while retrieving coach records.", ex);
            }

        }

      

        
    }
}





