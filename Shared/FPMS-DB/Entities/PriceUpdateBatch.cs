using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class PriceUpdateBatch
    {
        public int PriceUpdateBatchId { get; set; }
        public DateTime BatchExecutionDate { get; set; }
        public int TotalDispensor { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int BatchStatusId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
