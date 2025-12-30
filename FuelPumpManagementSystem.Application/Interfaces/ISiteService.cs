using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Request;

namespace FuelPumpManagementSystem.Application.Interfaces
{
    public interface ISiteService
    {
        Task<SiteDetailRequestDTO?> GetAsync();
        Task SaveAsync(SiteDetailRequestDTO request);
    }
}
