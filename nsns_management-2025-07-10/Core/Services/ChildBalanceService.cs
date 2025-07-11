using Core.Interfaces;
using Core.Models;
using Core.Repositories;
using Core.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{


    public class ChildBalanceService: IChildBalanceService
    {

        private readonly IChildBalanceRepository _balanceRepository;

        public ChildBalanceService(IChildBalanceRepository balanceRepository)
        {
            _balanceRepository = balanceRepository;
        }

        //When a parent buys a payment package
        public async Task<bool> AddPaymentToBalanceAsync(int childId, int paymentId, decimal amount, int createdBy)
        {
            try
            {
                bool result = await _balanceRepository.AddPaymentToBalanceAsync(childId, paymentId, amount, createdBy);
                return result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        //After a child finishes a course session
        public async Task<bool> DeductCourseSessionCostAsync(int enrollmentId,int createdBy)
        {
            try
            {
                bool result = await _balanceRepository.DeductCourseSessionCostAsync(enrollmentId, createdBy);
                return result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //After a child completes an activity
        public async Task<bool> DeductActivityCostAsync(int childId, int activityId, decimal cost, int createdBy)
        {
            try
            {
                bool result = await _balanceRepository.DeductActivityCostAsync(childId, activityId, cost, createdBy);
                return result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public async Task<IEnumerable<ChildBalanceViewModel>> GetBalanceHistoryAsync(int childId)
        {
            try
            {
                var balances = await _balanceRepository.GetBalanceHistoryAsync(childId);
                return balances;
            }
            catch (Exception ex)
            {
                return Enumerable.Empty<ChildBalanceViewModel>();
            }
            
        }


        public async Task<decimal> GetFinalBalanceAsync(int childId)
        {
            try
            {
                var balance = await _balanceRepository.GetFinalBalanceAsync(childId);
                return balance;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
    }


}