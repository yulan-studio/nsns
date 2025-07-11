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
    public interface IActivityEnrollmentService
    {












        Task<bool> AddRegisteredEnrollmentAsync(int childId, int activityId, string status, User user);

        Task<bool> RemoveRegisteredEnrollmentAsync(int enrollmentId);

        Task<IEnumerable<ActivityEnrollment>> GetRegisteredEnrollmentsByChildAsync(int childId);

        Task<IEnumerable<ActivityEnrollment>> GetCompletedEnrollmentsByChildAsync(int childId);

        Task<IEnumerable<ActivityEnrollment>> GetCanceledEnrollmentsByChildAsync(int childId);


        Task<IEnumerable<ActivityEnrollment>> UpdateActivityStatusToCompletedAsync();

        Task<bool> UpdateActivityStatusToCanceledAsync(int activityId);

        Task<bool> UpdateActivityStatusToClosedAsync(int activityId);


    }

}
