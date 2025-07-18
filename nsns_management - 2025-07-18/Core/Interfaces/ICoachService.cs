using Core.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface ICoachService
    {

        /// <summary>
        /// Adds a new admin user with the specified email and password.
        /// </summary>
        /// 
        
        Task<bool> AddAsync(string name, string email, string password, List<int> specialtyIds, string gender, string phone, string wechat, int cityId, User user);

        Task<bool> RemoveAsync(int coachId);

        Task<bool> UpdateAsync(int coachId, string name, string email, /*string password, */List<int> specialtyIds, string gender, string phone, string wechat, int cityId, User user);

        Task<Coach> GetAsync(int coachId);

        
        Task<IEnumerable<Coach>> GetAllAsync();

        Task<IEnumerable<Coach>> GetCoachesBySpecailtyAsync(int specialtyId);

       





    }


}
