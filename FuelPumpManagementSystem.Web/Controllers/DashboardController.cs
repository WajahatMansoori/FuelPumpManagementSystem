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
                Dispensers = new List<DispenserModel>(),
                Stats = new StatsModel()
            };

            // Initialize with empty dispensers to prevent null reference
            // Data will be populated via JavaScript/SignalR in real-time
            
            return View(model);
        }
    }
}
