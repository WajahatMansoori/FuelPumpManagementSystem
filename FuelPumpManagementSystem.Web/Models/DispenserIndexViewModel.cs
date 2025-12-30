using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.DTOs.Response;

namespace FuelPumpManagementSystem.Web.Models
{
    public class DispenserIndexViewModel
    {
        public ConfigureDispenserRequestDTO Configure { get; set; }
        public List<DispenserResponseDTO> Dispensers { get; set; }
    }
}
