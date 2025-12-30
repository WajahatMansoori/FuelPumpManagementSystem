using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class Dispenser
    {
        public int DispenserId { get; set; }
        public string? ApiEndPoint { get; set; }
        public bool IsOnline { get; set; } = true;
        public bool IsLocked { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<DispenserNozzle> Nozzles { get; set; }
    }
}
