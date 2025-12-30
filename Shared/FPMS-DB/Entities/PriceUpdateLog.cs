using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class PriceUpdateLog
    {
        public int PriceUpdateLogId { get; set; }
        public int DispensorId { get; set; }
        public string? ApiRequest { get; set; }
        public string? ApiResponse { get; set; }
        public string? Message { get; set; }
        public bool IsErrorOccured { get; set; }
        public int PriceUpdateBatchId { get; set; }
        public bool IsRecallAndResolve { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
