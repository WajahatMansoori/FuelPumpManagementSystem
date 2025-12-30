using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.DTOs.Response;

namespace FuelPumpManagementSystem.Application.Interfaces
{
    public interface IDispenserService
    {
        Task ConfigureDispenserAsync(ConfigureDispenserRequestDTO request);
        Task<List<DispenserResponseDTO>> GetAllAsync();
    }
}
