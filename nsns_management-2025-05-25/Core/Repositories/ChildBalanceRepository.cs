using Core.Interfaces;
using Core.Models;
using Core.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Core.ViewModels;




namespace Core.Repositories
{
    public class ChildBalanceRepository : IChildBalanceRepository
    {

        private readonly AppDbContext _context;

        // Constructor to inject DbContext
        public ChildBalanceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddPaymentToBalanceAsync(int childId, int paymentId, decimal amount, int createdBy)
        {
            decimal latestBalance = await GetFinalBalanceAsync(childId);

            var newEntry = new ChildBalance
            {
                ChildID = childId,
                PaymentID = paymentId,
                BalanceChange = amount,
                Balance = latestBalance + amount,
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
                UpdatedDate = DateTime.Now
            };

            _context.ChildBalances.Add(newEntry);
            return await _context.SaveChangesAsync() > 0;
        }


        public async Task<bool> DeductCourseSessionCostAsync(int enrollmentId,  int createdBy)
        {
            var enrollment = await _context.CourseEnrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

            decimal latestBalance = await GetFinalBalanceAsync(enrollment.ChildID);

            

            // Calculate income (replace with your logic — hardcoded here as example)
            decimal costForThisSession = enrollment.Course.HourlyCost * (decimal)enrollment.ActualHours;


            if (enrollment == null || enrollment.Status != "Completed")
                return false;

            var newEntry = new ChildBalance
            {
                ChildID = enrollment.ChildID,
                CourseID = enrollment.CourseID,
                EnrollmentID = enrollmentId,
                BalanceChange = costForThisSession*(-1),
                Balance = latestBalance - costForThisSession,
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
                UpdatedDate = DateTime.Now
            };

            _context.ChildBalances.Add(newEntry);
            return await _context.SaveChangesAsync() > 0;
        }



        public async Task<bool> DeductActivityCostAsync(int childId, int activityId, decimal cost, int createdBy)
        {
            decimal latestBalance = await GetFinalBalanceAsync(childId);

            var newEntry = new ChildBalance
            {
                ChildID = childId,
                ActivityID = activityId,
                BalanceChange = -cost,
                Balance = latestBalance - cost,
                CreatedDate = DateTime.Now,
                //CreatedBy = createdBy,
                //UpdatedBy = createdBy,
                UpdatedDate = DateTime.Now
            };

            _context.ChildBalances.Add(newEntry);
            return await _context.SaveChangesAsync()>0;
        }


       


        public async Task<List<ChildBalanceViewModel>> GetBalanceHistoryAsync(int childId)
        {
            var history = await _context.ChildBalances
                .Where(cb => cb.ChildID == childId)
                .OrderBy(cb => cb.CreatedDate)
                .Select(cb => new ChildBalanceViewModel
                {
                    CreatedDate = cb.CreatedDate,
                    Type = cb.PaymentID != null ? "Payment" :
                           cb.CourseID != null ? "Course Session" :
                           cb.ActivityID != null ? "Activity" : "Other",
                    CourseName = cb.CourseID != null ? cb.Course.Title : null,
                    ActivityName = cb.ActivityID != null ? cb.Activity.Title : null,
                    BalanceChange = cb.BalanceChange ?? 0,
                    Balance = cb.Balance ?? 0
                })
                .ToListAsync();

            return history;
        }


        //public async Task<decimal> GetFinalBalanceAsync(int childId)
        //{
        //    var latest = await _context.ChildBalances
        //        .Where(cb => cb.ChildID == childId)
        //        .OrderByDescending(cb => cb.CreatedDate)
        //        .FirstOrDefaultAsync();

        //    return latest?.Balance ?? 0;
        //}


        public async Task<decimal> GetFinalBalanceAsync(int childId)
        {
            return await _context.ChildBalances
                .Where(cb => cb.ChildID == childId)
                .OrderByDescending(cb => cb.CreatedDate)
                .Select(cb => cb.Balance ?? 0)
                .FirstOrDefaultAsync();
        }
    }


}
