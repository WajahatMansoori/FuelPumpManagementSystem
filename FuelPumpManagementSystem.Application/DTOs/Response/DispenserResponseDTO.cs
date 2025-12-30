using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Application.DTOs.Response
{
    public class DispenserResponseDTO
    {
        public int DispenserId { get; set; }
        public string ApiEndPoint { get; set; }

        public bool Nozzle1Enabled { get; set; }
        public bool Nozzle2Enabled { get; set; }

        public int? Nozzle1ProductTypeId { get; set; }
        public int? Nozzle2ProductTypeId { get; set; }
        public string? Nozzle1ProductTypeName { get; set; }
        public string? Nozzle2ProductTypeName { get; set; }

    }
}
