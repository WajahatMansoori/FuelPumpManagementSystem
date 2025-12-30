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
        private readonly IProductService _productService;

        public DispenserController(IDispenserService dispenserService, IProductService productService)
        {
            _dispenserService = dispenserService;
            _productService = productService;
        }
        public async Task<IActionResult> Index(int? id)
        {
            var dispensers = await _dispenserService.GetAllAsync();
            var products = await _productService.GetAllAsync();
            var configure = new ConfigureDispenserRequestDTO();

            if (id.HasValue)
            {
                var selected = dispensers.FirstOrDefault(d => d.DispenserId == id.Value);
                if (selected != null)
                {
                    configure = new ConfigureDispenserRequestDTO
                    {
                        DispenserId = selected.DispenserId,
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
                Dispensers = dispensers,
                Products = products
            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> Configure(DispenserIndexViewModel model)
        {
            if (model.Configure.DispenserId.HasValue && model.Configure.DispenserId.Value > 0)
            {
                await _dispenserService.UpdateDispenserAsync(model.Configure);
            }
            else
            {
                await _dispenserService.ConfigureDispenserAsync(model.Configure);
            }
            return RedirectToAction(nameof(Index));
        }
      
    }
}
