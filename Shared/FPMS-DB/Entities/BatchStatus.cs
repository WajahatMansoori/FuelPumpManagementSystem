using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class BatchStatus
    {
        public int BatchStatusId { get; set; }
        public string? BatchStatusName { get; set; } //inprogress, completed
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
