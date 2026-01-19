using Microsoft.AspNetCore.Mvc;
using FuelPumpManagementSystem.Web.Models;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                Dispensers = new List<DispenserModel>
                {
                    new DispenserModel
                    {
                        Id = 1,
                        UnitNumber = 1,
                        Status = "ONLINE",
                        IsLocked = false,
                        Nozzle1 = new NozzleModel
                        {
                            Id = 1,
                            FuelType = "PETROL",
                            Liters = 19.456m,
                            Price = 5116.00m,
                            PricePerLiter = 263.00m,
                            TotalLiters = 18560.723m,
                            IsFueling = false,
                            Color = "green"
                        },
                        Nozzle2 = new NozzleModel
                        {
                            Id = 2,
                            FuelType = "DIESEL",
                            Liters = 25.340m,
                            Price = 6585.00m,
                            PricePerLiter = 260.00m,
                            TotalLiters = 22340.560m,
                            IsFueling = true,
                            Color = "blue"
                        }
                    },
                    new DispenserModel
                    {
                        Id = 2,
                        UnitNumber = 1,
                        Status = "OFFLINE",
                        IsLocked = true,
                        Nozzle1 = new NozzleModel
                        {
                            Id = 3,
                            FuelType = "PETROL",
                            Liters = 21.892m,
                            Price = 5369.00m,
                            PricePerLiter = 279.00m,
                            TotalLiters = 20870.723m,
                            IsFueling = false,
                            Color = "green"
                        },
                        Nozzle2 = new NozzleModel
                        {
                            Id = 4,
                            FuelType = "HI-OCTANE",
                            Liters = 18.234m,
                            Price = 4890.00m,
                            PricePerLiter = 268.00m,
                            TotalLiters = 19456.890m,
                            IsFueling = false,
                            Color = "gold"
                        }
                    },
                    new DispenserModel
                    {
                        Id = 3,
                        UnitNumber = 2,
                        Status = "ONLINE",
                        IsLocked = false,
                        Nozzle1 = new NozzleModel
                        {
                            Id = 5,
                            FuelType = "DIESEL",
                            Liters = 30.125m,
                            Price = 7832.00m,
                            PricePerLiter = 260.00m,
                            TotalLiters = 25678.450m,
                            IsFueling = false,
                            Color = "blue"
                        },
                        Nozzle2 = new NozzleModel
                        {
                            Id = 6,
                            FuelType = "PETROL",
                            Liters = 22.567m,
                            Price = 5935.00m,
                            PricePerLiter = 263.00m,
                            TotalLiters = 21234.890m,
                            IsFueling = false,
                            Color = "green"
                        }
                    }
                },
                Stats = new StatsModel
                {
                    TotalPetrolSales = 125000.50m,
                    TotalDieselSales = 98500.75m,
                    TotalHiOctaneSales = 45600.25m,
                    TotalRevenue = 269101.50m,
                    ActiveDispensers = 2,
                    LastUpdated = DateTime.Now
                }
            };

            // Data will be updated via JavaScript/SignalR in real-time
            
            return View(model);
        }
    }
}
