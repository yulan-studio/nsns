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
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Get all payments
        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _context.Payments
                .Include(p => p.Parent)
                .Include(p => p.PaymentPackage)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByChildAsync(int childId)
        {
            return await _context.Payments
                .Include(p => p.Parent)
                .Include(p => p.PaymentPackage)
                .Include(p => p.Parent.ParentChild) // ✅ Include ParentChild relationship
                .ThenInclude(pc => pc.Child) // ✅ Include Child entity
                .Where(p => _context.ParentChild.Any(pc => pc.ChildID == childId && pc.ParentID == p.ParentID)) // ✅ Filters by childId using ParentChild
                .ToListAsync();
        }

        // 🔹 Get payment by ID
        public async Task<Payment> GetByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Parent)
                .Include(p => p.PaymentPackage)
                .FirstOrDefaultAsync(p => p.PaymentID == id);
        }

        public async Task<IEnumerable<Payment>> GetByPackageAsync(int packageId)
        {
            return await _context.Payments
                .Include(p => p.PaymentPackage)
                .Where(p => p.PaymentPackage.PackageID == packageId)
                .ToListAsync();
        }


        public async Task<Child> GetChildByIdAsync(int childId)
        {
            return await _context.Children
                .Where(c => c.ChildID == childId)
                .FirstOrDefaultAsync();
        }


        public async Task<IEnumerable<Parent>> GetParentsByChildAsync(int childId)
        {
            return await _context.ParentChild
                .Where(pc => pc.ChildID == childId)
                .Select(pc => pc.Parent)
                .ToListAsync();
        }

        public async Task<IEnumerable<PaymentPackage>> GetAllActivePackagesAsync()
        {
            return await _context.PaymentPackages
                .Where(p => p.IsActive) // ✅ Only fetch active packages
                .ToListAsync();
        }

        // 🔹 Add a new payment
        public async Task<int> AddAndReturnIdAsync(Payment payment)
        {
            
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment.PaymentID;
           
         
        }

        // 🔹 Update an existing payment
        public async Task<bool> UpdateAsync(Payment payment)
        {
            try
            {
                var existingPayment = await _context.Payments.FindAsync(payment.PaymentID);
                if (existingPayment == null) return false;

                _context.Entry(existingPayment).CurrentValues.SetValues(payment);
                var changes = await _context.SaveChangesAsync();
                return changes >= 0; // Returns true even if 0 rows were affected
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 🔹 Delete a payment
        public async Task<bool> RemoveAsync(int id)
        {
            var payment = await GetByIdAsync(id);
            if (payment == null) return false;

            try
            {
                _context.Payments.Remove(payment);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
