using System;

namespace FuelPumpManagementSystem.Web.Models
{
    public class DispenserStatusViewModel
    {
        public int DispenserId { get; set; }
        public string? DispenserName { get; set; }
        public string? ApiEndPoint { get; set; }
        public bool IsOnline { get; set; }
        public bool HasUpdateLog { get; set; }
        public bool IsErrorOccured { get; set; }
        public string? Message { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
