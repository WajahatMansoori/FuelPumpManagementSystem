using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Application.DTOs.Request
{
    public class ConfigureDispenserRequestDTO
    {
        public string? ApiEndPoint { get; set; }
        public bool IsNozzle1Enabled { get; set; }
        public bool IsNozzle2Enabled { get; set; }
        public int? Nozzle1ProductTypeId { get; set; }
        public int? Nozzle2ProductTypeId { get; set; }
    }
}
