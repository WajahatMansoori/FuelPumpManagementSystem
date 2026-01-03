using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Application.DTOs.Request
{
    public class UpdateProductPriceRequestDTO
    {
        public int ProductId { get; set; }
        public decimal NewPrice { get; set; }
    }
}
