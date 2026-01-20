namespace FuelPumpManagementSystem.Web.Models
{
    public class DashboardViewModel
    {
        public List<DispenserModel> Dispensers { get; set; }
        public StatsModel Stats { get; set; }

        public DashboardViewModel()
        {
            Dispensers = new List<DispenserModel>();
            Stats = new StatsModel();
        }
    }

    /// <summary>
    /// Model representing a single dispenser unit
    /// </summary>
    public class DispenserModel
    {
        public int Id { get; set; }
        public int UnitNumber { get; set; }
        public string Status { get; set; } // "ONLINE" or "OFFLINE"
        public bool IsLocked { get; set; }
        public NozzleModel Nozzle1 { get; set; }
        public NozzleModel Nozzle2 { get; set; }

        public DispenserModel()
        {
            Status = "ONLINE";
            IsLocked = false;
            Nozzle1 = new NozzleModel();
            Nozzle2 = new NozzleModel();
        }
    }

    /// <summary>
    /// Model representing a single nozzle on a dispenser
    /// </summary>
    public class NozzleModel
    {
        public int Id { get; set; }
        public string FuelType { get; set; } // "PETROL", "DIESEL", "HI-OCTANE"
        public decimal Liters { get; set; }
        public decimal Price { get; set; }
        public decimal PricePerLiter { get; set; }
        public decimal TotalLiters { get; set; }
        public string Status { get; set; } // "IN", "FUELING", "OUT"
        public bool IsEnabled { get; set; }
        public string Color { get; set; } // "green", "blue", "gold"

        public NozzleModel()
        {
            FuelType = "PETROL";
            Liters = 0;
            Price = 0;
            PricePerLiter = 263;
            TotalLiters = 0;
            Status = "IN";
            IsEnabled = true;
            Color = "green";
        }
    }

    /// <summary>
    /// Model for dashboard statistics
    /// </summary>
    public class StatsModel
    {
        public decimal TotalPetrolSales { get; set; }
        public decimal TotalDieselSales { get; set; }
        public decimal TotalHiOctaneSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveDispensers { get; set; }
        public DateTime LastUpdated { get; set; }

        public StatsModel()
        {
            LastUpdated = DateTime.Now;
        }
    }
}
