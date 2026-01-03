using FuelPumpManagementSystem.Application.DTOs.Request;
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

        [HttpPost]
        public async Task<IActionResult> UpdatePrices([FromBody] List<UpdateProductPriceRequestDTO> priceUpdates)
        {
            try
            {
                if (priceUpdates == null || !priceUpdates.Any())
                {
                    return BadRequest(new { success = false, message = "No price updates provided" });
                }

                var result = await _productService.UpdateProductPricesAsync(priceUpdates);

                if (result)
                {
                    return Ok(new { success = true, message = "Prices updated successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "No eligible dispensers found or all updates failed" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error updating prices: {ex.Message}" });
            }
        }
    }
}
