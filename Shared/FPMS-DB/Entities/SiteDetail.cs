using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.FPMS_DB.Entities
{
    public class SiteDetail
    {
        public int SiteDetailId { get; set; }
        public string? SiteName { get; set; }
        public string? SiteAddress { get; set; }
        public string? SitePhone { get; set; }
        public string? SiteLogo { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
    }
}
