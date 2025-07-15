using Core.Interfaces;
using Core.Models;
using Core.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;




namespace Core.Repositories
{
    public class CoachIncomeRepository : ICoachIncomeRepository
    {

        private readonly AppDbContext _context;

        // Constructor to inject DbContext
        public CoachIncomeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UpdateCoachIncomeAsync(int enrollmentId, int updatedBy)
        {
            var enrollment = await _context.CourseEnrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

            if (enrollment == null || enrollment.Status != "Completed")
                return false;

            var coachId = enrollment.Course.CoachID;
            var courseId = enrollment.Course.CourseID;


            // Calculate income (replace with your logic — hardcoded here as example)
            decimal incomeForThisSession = enrollment.Course.HourlyCost * (decimal)enrollment.ActualHours;

            // Get latest income for this coach
            decimal previousIncome = await _context.CoachIncomes
                .Where(i => i.CoachID == coachId)
                .OrderByDescending(i => i.IncomeID)
                .Select(i => i.Income ?? 0)
                .FirstOrDefaultAsync();

            var newIncome = previousIncome + incomeForThisSession;

            var incomeEntry = new CoachIncome
            {
                CoachID = coachId,
                CourseID = courseId,
                EnrollmentID = enrollmentId,
                IncomeChange = incomeForThisSession,
                Income = newIncome,
                CreatedDate = DateTime.Now,
                CreatedBy = updatedBy
            };

            _context.CoachIncomes.Add(incomeEntry);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<IEnumerable<CoachIncome>> GetCoachIncomeAsync(int coachId)
        { 

            var incomeRecords = await _context.CoachIncomes
                .Where(i => i.CoachID == coachId)
                .Include(i => i.Enrollment)
                    .ThenInclude(e => e.Child)
                .Include(i => i.Course)
                .OrderBy(i => i.Enrollment.ScheduledAt)
                .ToListAsync();
            return incomeRecords;
        }
    }


}
