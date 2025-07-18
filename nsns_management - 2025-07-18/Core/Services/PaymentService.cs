using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Core.Repositories;
using System.Numerics;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Core.Services
{
    public class PaymentService : IPaymentService
    {

        private readonly IPaymentRepository _paymentRepository;
        private readonly IUserRepository<User> _userRepository;
        private readonly IChildRepository _childRepository;
        private readonly IParentRepository _parentRepository;

        public PaymentService(IPaymentRepository paymentRepository, IUserRepository<User> userRepository, IChildRepository childRepository, IParentRepository parentRepository)
        {
            _paymentRepository = paymentRepository;
            _userRepository = userRepository;
            _childRepository = childRepository;
            _parentRepository = parentRepository;
        }

        // 🔹 Get all payments
        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _paymentRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Payment>> GetByChildAsync(int childId)
        {
            return await _paymentRepository.GetByChildAsync(childId);
        }

        // 🔹 Get payment by ID
        public async Task<Payment> GetByIdAsync(int id)
        {
            return await _paymentRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Payment>> GetByPackageAsync(int packageId)
        {
            return await _paymentRepository.GetByPackageAsync(packageId);
        }


        //public async Task<Child> GetChildByIdAsync(int childId)
        //{
        //    return await _paymentRepository.GetChildByIdAsync(childId);
        //}


        public async Task<IEnumerable<Parent>> GetParentsByChildAsync(int childId)
        {
            return await _paymentRepository.GetParentsByChildAsync(childId);
        }

        public async Task<IEnumerable<PaymentPackage>> GetAllActivePackagesAsync()
        {
            return await _paymentRepository.GetAllActivePackagesAsync();
        }

        // 🔹 Add a new payment
        public async Task<int> AddAndReturnIdAsync(int childId, int parentId, int packageId, decimal amount, DateTime? paymentDate, string receiptPath, User user)
        {
            
            var createdBy = 1;
           

            var child = await _childRepository.GetAsync(childId);
            if (child == null)
            {
                throw new Exception("Child is not found.");
            }

            var parent = await _parentRepository.GetAsync(parentId);
            if (parent == null)
            {
                throw new Exception("Parent is not found.");
            }

            //var createdByUser = await _userRepository.GetAsync(createdBy);
            //if (createdByUser == null)
            //{
            //    throw new Exception("No createdBy is added.");
            //}

            var payment = new Payment
            {
                //ChildID = childId,
                ParentID = parentId,
                CreatedBy = createdBy,
                PaymentPackageID = packageId,
                Amount = amount,
                Parent = parent,
                Child = child,
                PaymentDate = paymentDate,
                CreatedByUser = user,
                Receipt = receiptPath
            };

            // Add the course to the repository
            try
            {
                return await _paymentRepository.AddAndReturnIdAsync(payment);
                
            }
            catch (Exception ex)
            {
                throw new Exception("No payment is added.");
            }
            
        }

        // 🔹 Update an existing payment
        public async Task<bool> UpdateAsync(Payment payment)
        {
            var existingPayment = await GetByIdAsync(payment.PaymentID);
            if (existingPayment == null)
                throw new Exception("Payment not found.");

            return await _paymentRepository.UpdateAsync(payment);
        }

        // 🔹 Delete a payment
        public async Task<bool> RemoveAsync(int paymentId)
        {
            var payment = await GetByIdAsync(paymentId);
            if (payment == null)
                throw new Exception("Payment not found.");

            return await _paymentRepository.RemoveAsync(paymentId);
        }
    }
}





