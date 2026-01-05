using FuelPumpManagementSystem.Application.DTOs.Response;
using FuelPumpManagementSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using Shared.FPMS_DB.Entities;
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

        public async Task<List<TransactionResponseDTO>> GetAllTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null, 
            string[] dispenserIds = null, string nozzleId = null, string[] productIds = null)
        {
            try
            {
                var query = _db.Transaction.AsQueryable();

                // Apply date filters
                if (fromDate.HasValue)
                {
                    query = query.Where(t => t.CreatedAt.Date >= fromDate.Value.Date);
                }
                if (toDate.HasValue)
                {
                    query = query.Where(t => t.CreatedAt.Date <= toDate.Value.Date);
                }

                // Apply dispenser filter
                if (dispenserIds != null && dispenserIds.Length > 0 && !dispenserIds.Contains("ALL"))
                {
                    var dispenserIdsInt = dispenserIds.Select(id => int.Parse(id)).ToList();
                    query = query.Where(t => dispenserIdsInt.Contains(t.DispenserId));
                }

                // Apply nozzle filter
                if (!string.IsNullOrEmpty(nozzleId) && nozzleId != "ALL")
                {
                    var nozzleIdInt = int.Parse(nozzleId);
                    query = query.Where(t => t.NozzleId == nozzleIdInt);
                }

                // Apply product filter
                if (productIds != null && productIds.Length > 0 && !productIds.Contains("ALL"))
                {
                    var productIdsInt = productIds.Select(id => int.Parse(id)).ToList();
                    query = query.Where(t => productIdsInt.Contains(t.ProductTypeId));
                }

                var transactions = await query
                    .Select(t => new TransactionResponseDTO
                    {
                        TransactionId = t.TransactionId,
                        DispenserId = t.DispenserId,
                        NozzleId = t.NozzleId,
                        Amount = t.Amount,
                        Liter = t.Liter,
                        UnitPrice = t.UnitPrice,
                        ProductTypeId = t.ProductTypeId,
                        CreatedAt = t.CreatedAt,
                        LastTotalCash = _db.DispenserNozzle
                            .Where(dn => dn.DispenserId == t.DispenserId && dn.NozzleId == t.NozzleId)
                            .Select(dn => dn.LastTotalCash)
                            .FirstOrDefault(),
                        LastTotalLitre = _db.DispenserNozzle
                            .Where(dn => dn.DispenserId == t.DispenserId && dn.NozzleId == t.NozzleId)
                            .Select(dn => dn.LastTotalLiter)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return transactions;
            }
            catch (Exception ex)
            {
                // Log exception here if needed
                throw new Exception("Error retrieving transactions", ex);
            }
        }
    }
}

