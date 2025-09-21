using Core.Models;
using Core.Repositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IFeeService
    {
        Task<bool> AddAsync(Fee fee);

        Task<bool> AddCourseFeeAsync(int enrollmentId, string paymentModel, decimal totalCost, string description, User user);

        Task<bool> AddAcitvityFeeAsync(int enrollmentId, string paymentModel, decimal totalCost, string description, User user);

        Task<bool> UpdateAsync(Fee fee);
        
        Task<bool> DeleteAsync(int feeId);
       
        Task<Fee> GetAsync(int feeId);

        Task<IEnumerable<Fee>> GetAllAsync();
       




    }


}
