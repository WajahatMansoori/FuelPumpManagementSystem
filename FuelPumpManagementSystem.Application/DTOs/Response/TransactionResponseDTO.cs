using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Application.DTOs.Response
{
    public class TransactionResponseDTO
    {
        public long TransactionId { get; set; }
        public int DispenserId { get; set; }
        public int NozzleId { get; set; }
        public decimal Amount { get; set; }
        public decimal Liter { get; set; }
        public decimal UnitPrice { get; set; }
        public int ProductTypeId { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal? LastTotalCash { get; set; }
        public decimal? LastTotalLitre { get; set; }
        public decimal? LastHardwareTotalCash { get; set; }
        public decimal? LastHardwareTotalLiter { get; set; }
    }

}
