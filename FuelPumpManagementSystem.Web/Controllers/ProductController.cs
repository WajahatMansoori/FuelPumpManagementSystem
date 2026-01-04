using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.Interfaces;
using FuelPumpManagementSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IDispenserService _dispenserService;
        private readonly FPMSDbContext _db;

        public ProductController(IProductService productService, IDispenserService dispenserService, FPMSDbContext db)
        {
            _productService = productService;
            _dispenserService = dispenserService;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllAsync();
            var dispensers = await _dispenserService.GetAllAsync();

            // Get only products that are mapped to at least one active dispenser nozzle
            var productsWithPrices = await _db.Product
                .Where(p => p.IsActive && _db.DispenserNozzle.Any(dn => dn.ProductId == p.ProductId && dn.IsActive))
                .Select(p => new ProductPriceViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductColorCode = p.ProductColorCode,
                    // Get the first non-null current price from any dispenser nozzle for this product
                    CurrentPrice = _db.DispenserNozzle
                        .Where(dn => dn.ProductId == p.ProductId && dn.IsActive && dn.CurrentProductPrice != null)
                        .Select(dn => dn.CurrentProductPrice!.Value)
                        .FirstOrDefault(),
                    IsMapped = true
                })
                .ToListAsync();

            // Get latest price update log for each dispenser
            var dispenserStatuses = await _db.Dispenser
                .Where(d => d.IsActive)
                .Select(d => new DispenserStatusViewModel
                {
                    DispenserId = d.DispenserId,
                    DispenserName = $"Dispenser #{d.DispenserId}",
                    ApiEndPoint = d.ApiEndPoint,
                    IsOnline = d.IsOnline,
                    HasUpdateLog = _db.PriceUpdateLog.Any(log => log.DispensorId == d.DispenserId && log.IsActive),
                    IsErrorOccured = _db.PriceUpdateLog
                        .Where(log => log.DispensorId == d.DispenserId && log.IsActive)
                        .OrderByDescending(log => log.CreatedAt)
                        .Select(log => log.IsErrorOccured)
                        .FirstOrDefault(),
                    Message = _db.PriceUpdateLog
                        .Where(log => log.DispensorId == d.DispenserId && log.IsActive)
                        .OrderByDescending(log => log.CreatedAt)
                        .Select(log => log.Message)
                        .FirstOrDefault(),
                    LastUpdated = _db.PriceUpdateLog
                        .Where(log => log.DispensorId == d.DispenserId && log.IsActive)
                        .OrderByDescending(log => log.CreatedAt)
                        .Select(log => log.CreatedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewBag.Products = productsWithPrices;
            ViewBag.DispenserStatuses = dispenserStatuses;
            ViewBag.HasPriceUpdateLogs = dispenserStatuses.Any(d => d.HasUpdateLog);

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
