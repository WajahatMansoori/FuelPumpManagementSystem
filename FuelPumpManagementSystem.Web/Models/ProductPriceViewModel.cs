using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Web.Models
{
    public class ProductPriceViewModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductColorCode { get; set; }
        public decimal CurrentPrice { get; set; }
        public bool IsMapped { get; set; }
    }
}
