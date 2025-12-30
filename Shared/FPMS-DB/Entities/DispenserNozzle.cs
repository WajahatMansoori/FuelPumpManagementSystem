using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class DispenserNozzle
    {
        public int DispenserNozzleId { get; set; }
        public int DispenserId { get; set; }
        public int NozzleId { get; set; }
        public int? ProductId { get; set; }
        public bool IsEnable { get; set; } = true;
        public decimal? LastTotalLiter { get; set; }
        public decimal? LastTotalCash { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public Dispenser? Dispenser { get; set; }
        public Product? Product { get; set; }
    }
}
