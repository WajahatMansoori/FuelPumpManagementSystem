using FuelPumpManagementSystem.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Application.Interfaces
{
    public interface ITransactionService
    {
        //Task<List<TransactionResponseDTO>> GetAllTransactionsAsync();
       
            Task<List<TransactionResponseDTO>> GetAllTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        

    }
}
