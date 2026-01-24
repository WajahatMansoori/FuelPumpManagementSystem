using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FuelPumpManagementSystem.Web.Models;
using Shared.FPMS_DB;
using FuelPumpManagementSystem.Application.DTOs.Request;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class DashboardController : Controller
    {
        private readonly FPMSDbContext _db;

        public DashboardController(FPMSDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            // Fetch all dispensers with at least one enabled nozzle, ordered sequentially by DispenserId
            var dispensersWithNozzles = await _db.Dispenser
                .Include(d => d.Nozzles)
                .Where(d => d.IsActive && d.Nozzles.Any(n => n.IsActive && n.IsEnable))
                .OrderBy(d => d.DispenserId)
                .ToListAsync();

            foreach (var dispenser in dispensersWithNozzles)
            {
                var dispenserModel = new DispenserModel
                {
                    Id = dispenser.DispenserId,
                    UnitNumber = dispenser.DispenserId,
                    Status = dispenser.IsOnline ? "ONLINE" : "OFFLINE",
                    IsLocked = dispenser.IsLocked
                };

                // Get live status for both nozzles
                var liveStatuses = await _db.DispenserLiveStatus
                    .Where(ls => ls.DispenserId == dispenser.DispenserId)
                    .ToListAsync();

                // Process Nozzle 1
                var nozzle1LiveStatus = liveStatuses.FirstOrDefault(ls => ls.NozzleId == 1);
                var nozzle1Config = dispenser.Nozzles.FirstOrDefault(n => n.NozzleId == 1 && n.IsActive);
                
                // Always create nozzle model (enabled or disabled)
                dispenserModel.Nozzle1 = MapNozzleModel(nozzle1LiveStatus, nozzle1Config);

                // Process Nozzle 2
                var nozzle2LiveStatus = liveStatuses.FirstOrDefault(ls => ls.NozzleId == 2);
                var nozzle2Config = dispenser.Nozzles.FirstOrDefault(n => n.NozzleId == 2 && n.IsActive);
                
                // Always create nozzle model (enabled or disabled)
                dispenserModel.Nozzle2 = MapNozzleModel(nozzle2LiveStatus, nozzle2Config);

                model.Dispensers.Add(dispenserModel);
            }

            // Calculate stats from transactions
            var today = DateTime.Today;
            var todayTransactions = await _db.Transaction
                .Where(t => t.IsActive && t.CreatedAt >= today)
                .ToListAsync();

            model.Stats = new StatsModel
            {
                TotalPetrolSales = todayTransactions.Where(t => t.ProductTypeId == 1).Sum(t => t.Liter),
                TotalDieselSales = todayTransactions.Where(t => t.ProductTypeId == 3).Sum(t => t.Liter),
                TotalHiOctaneSales = todayTransactions.Where(t => t.ProductTypeId == 2).Sum(t => t.Liter),
                TotalRevenue = todayTransactions.Sum(t => t.Amount),
                ActiveDispensers = dispensersWithNozzles.Count(d => d.IsOnline),
                LastUpdated = DateTime.Now
            };

            // Fetch site details from database
            var siteDetail = await _db.SiteDetail
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (siteDetail != null)
            {
                model.SiteDetail = new SiteDetailModel
                {
                    SiteName = siteDetail.SiteName ?? string.Empty,
                    SiteAddress = siteDetail.SiteAddress ?? string.Empty,
                    SitePhone = siteDetail.SitePhone ?? string.Empty,
                    SiteLogo = siteDetail.SiteLogo ?? string.Empty
                };
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ValidateLockAccessKey([FromBody] AccessKeyLoginViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.AccessKey))
                {
                    return Json(new { success = false, message = "Access key is required" });
                }

                // Check if access key matches any non-admin user password (IsAdminLogin = false)
                var isValid = await _db.User
                    .AnyAsync(u => u.Password == model.AccessKey && !u.IsAdminLogin);

                if (isValid)
                {
                    return Json(new { success = true, message = "Access granted" });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid Access Key" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private NozzleModel MapNozzleModel(Shared.FPMS_DB.Entities.DispenserLiveStatus? liveStatus, Shared.FPMS_DB.Entities.DispenserNozzle? nozzleConfig)
        {
            // Check if nozzle is enabled
            bool isEnabled = nozzleConfig?.IsEnable ?? false;

            var nozzleModel = new NozzleModel
            {
                Id = liveStatus?.DispenserLiveStatusId ?? 0,
                IsEnabled = isEnabled
            };

            // If disabled, set all values to defaults and status to DISABLED
            if (!isEnabled)
            {
                nozzleModel.Liters = 0;
                nozzleModel.Price = 0;
                nozzleModel.PricePerLiter = 0;
                nozzleModel.TotalLiters = 0;
                nozzleModel.FuelType = "N/A";
                nozzleModel.Color = "gray";
                nozzleModel.Status = "DISABLED";
                return nozzleModel;
            }

            // If enabled, populate with live data
            nozzleModel.Liters = liveStatus?.CurrentLiter ?? 0;
            nozzleModel.Price = liveStatus?.CurrentAmount ?? 0;
            nozzleModel.PricePerLiter = liveStatus?.UnitPrice ?? 0;
            nozzleModel.TotalLiters = liveStatus?.HardwareTotalLiter ?? 0;

            // Map ProductTypeId to FuelType and Color
            int productTypeId = liveStatus?.ProductTypeId ?? nozzleConfig?.ProductId ?? 0;
            
            switch (productTypeId)
            {
                case 1: // Petrol
                    nozzleModel.FuelType = "PETROL";
                    nozzleModel.Color = "green";
                    break;
                case 2: // Hi-Octane
                    nozzleModel.FuelType = "HI-OCTANE";
                    nozzleModel.Color = "gold";
                    break;
                case 3: // Diesel
                    nozzleModel.FuelType = "DIESEL";
                    nozzleModel.Color = "blue";
                    break;
                case 4: // Spare 1
                    nozzleModel.FuelType = "SPARE 1";
                    nozzleModel.Color = "gray";
                    break;
                case 5: // Spare 2
                    nozzleModel.FuelType = "SPARE 2";
                    nozzleModel.Color = "gray";
                    break;
                case 6: // Spare 3
                    nozzleModel.FuelType = "SPARE 3";
                    nozzleModel.Color = "gray";
                    break;
                default:
                    nozzleModel.FuelType = "UNKNOWN";
                    nozzleModel.Color = "gray";
                    break;
            }

            // Map NozzleStatus to Status (IN/FUELING/OUT)
            if (liveStatus == null)
            {
                nozzleModel.Status = "IN";
            }
            else
            {
                nozzleModel.Status = liveStatus.NozzleStatus switch
                {
                    "IDLE" => "IN",
                    "FUELING" => "FUELING",
                    "Out" => "OUT",
                    _ => "IN"
                };
            }

            return nozzleModel;
        }
    }
}
