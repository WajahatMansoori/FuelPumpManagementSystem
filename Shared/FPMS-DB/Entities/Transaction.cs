using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public int DispenserId { get; set; }
        public int NozzleId { get; set; }
        public decimal Amount { get; set; }
        public decimal Liter { get; set; }
        public decimal UnitPrice { get; set; }
        public int ProductTypeId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
