using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class DispenserLiveStatus
    {
        public int DispenserLiveStatusId { get; set; }
        public int DispenserId { get; set; }
        public int NozzleId { get; set; }
        public int? ProductTypeId { get; set; }
        public string? NozzleStatus { get; set; }
        public decimal? CurrentLiter { get; set; }
        public decimal? CurrentAmount { get; set; }
        public decimal? HardwareTotalLiter { get; set; }
        public decimal? HardwareTotalCash { get; set; }
        public decimal? UnitPrice { get; set; }
        public bool IsOnline { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
