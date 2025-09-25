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

namespace Core.Services
{
    public class FeeService: IFeeService
    {
        private readonly IFeeRepository _feeRepository;

        public FeeService(IFeeRepository feeRepository)
        {
            _feeRepository = feeRepository;
        }

        // ✅ Add a City
        public async Task<bool> AddAsync(Fee fee)
        {
           
            return await _feeRepository.AddAsync(fee);
        }

        


        public async Task<bool> AddCourseFeeAsync(int enrollmentId, string paymentModel, decimal totalCost, string description, User user)
        {
            var fee = new Fee
            {
                CourseEnrollmentID = enrollmentId,
                PaymentModel = paymentModel,
                TotalCost = totalCost,
                Description = description,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsPaid = false
            };
            return await _feeRepository.AddAsync(fee);
        }


        public async Task<bool>  AddAcitvityFeeAsync(int enrollmentId, string paymentModel, decimal totalCost, string description, User user)
        {
            var fee = new Fee
            {
                ActivityEnrollmentID = enrollmentId,
                PaymentModel = paymentModel,
                TotalCost = totalCost,
                Description = description,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsPaid = false
            };
            return await _feeRepository.AddAsync(fee);
        }

        public async Task<bool> DeleteCourseFeeAsync(int enrollmentId)
        {
            if (enrollmentId <= 0)
                throw new ArgumentException("Invalid enrollment ID.");

            try
            {
                return await _feeRepository.DeleteCourseFeeAsync(enrollmentId);
              
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting course fee: {ex.Message}", ex);
            }
        }


        
        public async Task<Fee?> GetFeeForCourseEnrollmentAsync(int courseEnrollmentId)
        {
            return await _feeRepository.GetByCourseEnrollmentIdAsync(courseEnrollmentId);
        }

        public async Task<bool> UpdateFeeAsync(Fee fee, string description, decimal totalCost, int userId)
        {
            if (fee == null || fee.IsPaid)
                return false;

            fee.Description = description;
            fee.TotalCost = totalCost;
            fee.UpdatedAt = DateTime.UtcNow;
            fee.UpdatedBy = userId;

            await _feeRepository.UpdateAsync(fee);
            return true;
        }



        // ✅ Update a City
        //public async Task<bool> UpdateAsync(Fee fee)
        //{
        //    var existingFee = await _feeRepository.GetAsync(fee.FeeID);
        //    if (existingFee == null)
        //        throw new KeyNotFoundException("Fee not found.");

        //    existingFee.Description = fee.Description; // Update fields
        //    existingFee.TotalCost = fee.TotalCost;
        //    existingFee.CreatedAt = fee.CreatedAt;
        //    existingFee.IsPaid = fee.IsPaid;
        //    existingFee.PaidAt = fee.PaidAt;

        //    return await _feeRepository.UpdateAsync(fee);
        //}

        // ✅ Delete a City
        public async Task<bool> DeleteAsync(int feeId)
        {
            var fee = await _feeRepository.GetAsync(feeId);
            if (fee == null)
                throw new KeyNotFoundException("Fee not found.");
            return await _feeRepository.RemoveAsync(fee);
        }

        // ✅ Get City by ID
        public async Task<Fee> GetAsync(int feeId)
        {
            return await _feeRepository.GetAsync(feeId) ?? throw new KeyNotFoundException("Fee not found.");
        }

        // ✅ Get All Cities
        public async Task<IEnumerable<Fee>> GetAllAsync()
        {
            try
            {
                // Fetch all coach records from the repository
                var feeList = await _feeRepository.GetAllAsync();

                // You can add additional logic or transformations here if necessary
                return feeList;
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed (e.g., logging)
                throw new Exception("An error occurred while retrieving fee records.", ex);
            }
        }


      

    }
}
