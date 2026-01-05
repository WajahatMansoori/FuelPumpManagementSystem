using FuelPumpManagementSystem.Application.DTOs.Response;

namespace FuelPumpManagementSystem.Web.Models
{
    public class TransactionIndexViewModel
    {
        public List<TransactionResponseDTO> Transactions { get; set; }
        public List<ProductResponseDTO> Products { get; set; }
    }
}
