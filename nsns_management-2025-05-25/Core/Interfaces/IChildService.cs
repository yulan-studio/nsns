using Core.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IChildService
    {
        Task<IEnumerable<Child>> GetAllAsync();
        Task<Child> GetAsync(int childId);

        Task<Child?> GetByIdAsync(int userId);

        //Task<Child?> GetChildByIdAsync(int childId);

        Task<bool> AddAsync(string name, DateTime? birthDate, string? gender, int? cityId, string email, string password, User user);
        //Task<bool> UpdateAsync(Child child);
        Task<bool> UpdateAsync(int childId, string name, DateTime birthDate, string gender, int cityId, string email/*, string password*/);

        Task<bool> RemoveAsync(int childId);

        Task<bool> CheckRegisteredAsync(int childId);

        Task<bool> CheckPaidAsync(int childId);
    }


}
