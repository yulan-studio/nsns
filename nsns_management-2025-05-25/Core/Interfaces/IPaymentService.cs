using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<Payment> GetByIdAsync(int id);

        Task<IEnumerable<Payment>> GetByPackageAsync(int packageId);

        Task<IEnumerable<Payment>> GetByChildAsync(int childId);

        //Task<Child> GetChildByIdAsync(int childId);

        Task<IEnumerable<Parent>> GetParentsByChildAsync(int childId);

        Task<IEnumerable<PaymentPackage>> GetAllActivePackagesAsync();
        //Task<bool> AddAsync(int childId, int parentId, int packageId, decimal amount, DateTime? paymentDate, string receiptPath, User user);
        Task<int> AddAndReturnIdAsync(int childId, int parentId, int packageId, decimal amount, DateTime? paymentDate, string receiptPath, User user);
        Task<bool> UpdateAsync(Payment payment);
        Task<bool> RemoveAsync(int paymentId);
    }
}
