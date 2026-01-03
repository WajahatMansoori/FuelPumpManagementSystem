using FuelPumpManagementSystem.Application.Interfaces;
using Shared.FPMS_DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly FPMSDbContext _db;

        public TransactionService(FPMSDbContext db)
        {
            _db = db;
        }
    }
}
