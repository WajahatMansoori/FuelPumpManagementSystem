using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class DispenserActionLog
    {
        public int DispenserActionLogId { get; set; }
        public int DispenserId { get; set; }
        public int DispenserActionTypeId { get; set; }
        public string? ApiRequest { get; set; }
        public string? ApiResponse { get; set; }
        public string? Message { get; set; }
        public bool IsErrorOccured { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
    }
}
