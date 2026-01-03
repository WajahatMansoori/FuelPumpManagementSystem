using FuelPumpManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IDispenserService _dispenserService;

        public ProductController(IProductService productService, IDispenserService dispenserService)
        {
            _productService = productService;
            _dispenserService = dispenserService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllAsync();
            var dispensers = await _dispenserService.GetAllAsync();

            ViewBag.Products = products;
            ViewBag.Dispensers = dispensers;

            return View();
        }
    }
}
