using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.Interfaces;
using FuelPumpManagementSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class DispenserController : Controller
    {
        private readonly IDispenserService _dispenserService;
        public DispenserController(IDispenserService dispenserService)
        {
            _dispenserService = dispenserService;
        }
        public async Task<IActionResult> Index(int? id)
        {
            var dispensers = await _dispenserService.GetAllAsync();
            var configure = new ConfigureDispenserRequestDTO();

            if (id.HasValue)
            {
                var selected = dispensers.FirstOrDefault(d => d.DispenserId == id.Value);
                if (selected != null)
                {
                    configure = new ConfigureDispenserRequestDTO
                    {
                        ApiEndPoint = selected.ApiEndPoint,
                        IsNozzle1Enabled = selected.Nozzle1Enabled,
                        IsNozzle2Enabled = selected.Nozzle2Enabled,
                        Nozzle1ProductTypeId = selected.Nozzle1ProductTypeId,
                        Nozzle2ProductTypeId = selected.Nozzle2ProductTypeId
                    };
                }
            }

            var vm = new DispenserIndexViewModel
            {
                Configure = configure,
                Dispensers = dispensers
            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> Configure(DispenserIndexViewModel model)
        {
            bool nozzle1 = model.Configure.IsNozzle1Enabled;
            bool nozzle2 = model.Configure.IsNozzle2Enabled;

            await _dispenserService.ConfigureDispenserAsync(model.Configure);
            return RedirectToAction(nameof(Index));
        }
      
    }
}
