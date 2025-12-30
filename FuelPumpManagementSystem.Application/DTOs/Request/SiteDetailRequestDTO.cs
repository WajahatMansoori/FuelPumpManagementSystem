using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Application.DTOs.Request
{
    public class SiteDetailRequestDTO
    {
        public string? SiteName { get; set; }
        public string? SiteAddress { get; set; }
        public string? SitePhone { get; set; }
        public string? SiteLogo { get; set; }
    }
}
