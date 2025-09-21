using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IFeeRepository 
    {
        Task<bool> AddAsync(Fee entity);
       

        // Remove a Specialty
        Task<bool> RemoveAsync(Fee entity);
        

        // Update a Specialty
        Task<bool> UpdateAsync(Fee entity);


        // Get a Specialty by ID
        Task<Fee> GetAsync(int id);

        Task<IEnumerable<Fee>> GetAllAsync();


    }
}
