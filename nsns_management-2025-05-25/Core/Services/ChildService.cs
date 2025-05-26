using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Core.Repositories;
using System.Numerics;
using System.Xml.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Core.Services
{
    public class ChildService : IChildService
    {
       
      
        private readonly IChildRepository _childRepository;
        private readonly ICityRepository _cityRepository;
        private readonly IUserRepository<User> _userRepository;
        private readonly IUserRegistrationService _userRegistrationService;
        private readonly UserManager<Core.Models.User> _userManager;

        public ChildService(IChildRepository childRepository, ICityRepository cityRepository, IUserRepository<User> userRepository, IUserRegistrationService userRegistrationService, UserManager<Core.Models.User> userManager)
        {
            _childRepository = childRepository;
            _cityRepository = cityRepository;
            _userRepository = userRepository;
            _userRegistrationService = userRegistrationService;
            _userManager = userManager;

        }

        public async Task<IEnumerable<Child>> GetAllAsync()
        {
            return await _childRepository.GetAllAsync();
        }

        public async Task<Child> GetAsync(int childId)
        {
            var child = await _childRepository.GetAsync(childId);
            if (child == null)
            {
                throw new KeyNotFoundException($"Child with ID {childId} not found.");
            }
            return child;
        }

        public async Task<Child?> GetByIdAsync(int userId)
        {
            var user = await _userRepository.GetAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"Child not found.");
            }
            var child = await _childRepository.GetByIdAsync(userId);
            if (child == null)
            {
                throw new KeyNotFoundException($"Child not found.");
            }
            return child;
        }


        public async Task<bool> AddAsync(string name, DateTime? birthDate, string? gender, int? cityId, string email, string password, User user)
        {

            // Check if a user with the same username or email already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                throw new Exception("A user with the same username or email already exists.");
            }

            var result = await _userRegistrationService.RegisterUserAsync(email, password, "Child", user);

            if (result == true)
            {
                //var user = await _userRepository.GetByEmailAsync(email);
                var newUser = await _userManager.FindByEmailAsync(email);
                if (newUser != null)
                {
                    City city = null;
                    if (cityId != null)
                        city = await _cityRepository.GetAsync((int)cityId);
                    var childUser = new Child
                    {
                        Name = name,
                        BirthDate = birthDate,
                        Gender = gender,
                        User = newUser,
                        City = city
                    };


                    return await _childRepository.AddAsync(childUser);
                }
                else return false;
            }
            else
                return false;

            
        }

        public async Task<bool> UpdateAsync(Child child)
        {
            //var existingChild = await _childRepository.GetAsync(child.ChildID);
            //if (existingChild == null)
            //    throw new Exception("Child not found.");

            return await _childRepository.UpdateAsync(child);
        }



        public async Task<bool> UpdateAsync(int childId, string name, DateTime birthDate, string gender, int cityId, string email/*, string password*/)
        {
            // Find the coach by ID
            var child = await _childRepository.GetAsync(childId);
            if (child == null)
            {
                throw new KeyNotFoundException($"Child with ID {childId} not found.");
            }

            // Update fields
            child.Name = name;
            child.User.Email = email;
            child.BirthDate = birthDate;
            child.Gender = gender;
           
            child.CityID = cityId;
            child.User.UpdatedDate = DateTime.Now;

            // Update the password if provided
            //if (!string.IsNullOrWhiteSpace(password))
            //{
            //    coach.Password = _passwordHasher.HashPassword(coach, password);
            //}

            // Save changes
            return await _childRepository.UpdateAsync(child);
        }

        public async Task<bool> RemoveAsync(int childId)
        {

            // Find the child by ID
            var child = await _childRepository.GetAsync(childId);
            if (child == null)
            {
                throw new KeyNotFoundException($"Child with ID {childId} not found.");
            }

            // Remove the child
            var result = await _childRepository.RemoveAsync(child);


            if (result)
                result = await _userRepository.RemoveAsync(child.User);
            return result;
        }

        public async Task<bool> CheckRegisteredAsync(int childId)
        {
            var result = await _childRepository.CheckRegisteredAsync(childId);
            return result;
        }

        public async Task<bool> CheckPaidAsync(int childId)
        {
            var result = await _childRepository.CheckPaidAsync(childId);
            return result;
        }

    }
}





