using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
//using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Core.Services
{
    public class CoachIncomeService: ICoachIncomeService
    {
        private readonly ICoachIncomeRepository _coachIncomeRepository;
        public CoachIncomeService(ICoachIncomeRepository coachIncomeRepository) 
        {
            _coachIncomeRepository = coachIncomeRepository;
        }
        public async Task<bool> UpdateCoachIncomeAsync(int enrollmentId, int updatedBy)
        {
            try
            {
                bool result = await _coachIncomeRepository.UpdateCoachIncomeAsync(enrollmentId, updatedBy);
                return result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public async Task<IEnumerable<CoachIncome>> GetCoachIncomeAsync(int coachId)
        {
            try
            {
                IEnumerable<CoachIncome> incomes = await _coachIncomeRepository.GetCoachIncomeAsync(coachId);
                return incomes;
            }
            catch (Exception ex)
            {
                return Enumerable.Empty<CoachIncome>();
            }
        }
    }
}
