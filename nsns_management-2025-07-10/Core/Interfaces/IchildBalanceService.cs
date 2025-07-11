using Core.Models;
using Core.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IChildBalanceService
    {

        Task<bool> AddPaymentToBalanceAsync(int childId, int paymentId, decimal amount, int createdBy);
        Task<bool> DeductCourseSessionCostAsync(int enrollmentId, int createdBy);
        Task<bool> DeductActivityCostAsync(int childId, int activityId, decimal cost, int createdBy);
        Task<IEnumerable<ChildBalanceViewModel>> GetBalanceHistoryAsync(int childId);
        Task<decimal> GetFinalBalanceAsync(int childId);
    }


}
