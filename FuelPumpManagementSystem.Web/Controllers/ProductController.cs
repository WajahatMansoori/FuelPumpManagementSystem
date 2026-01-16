using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.Interfaces;
using FuelPumpManagementSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using System.Security.Cryptography;
using System.Text;

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
            // Check if user is authenticated for product management
            var isAuthenticated = HttpContext.Session.GetString("ProductAccessAuthenticated");
            if (string.IsNullOrEmpty(isAuthenticated))
            {
                return View("AccessKeyLogin");
            }

            var products = await _productService.GetAllAsync();
            var dispensers = await _dispenserService.GetAllAsync();

            // Get all active products regardless of dispenser mapping
            var productsWithPrices = await _db.Product
                .Where(p => p.IsActive)
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
                    // Check if product is mapped to any active dispenser nozzle
                    IsMapped = _db.DispenserNozzle.Any(dn => dn.ProductId == p.ProductId && dn.IsActive)
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
        public async Task<IActionResult> ValidateAccessKey([FromBody] AccessKeyLoginViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.AccessKey))
                {
                    return Json(new { success = false, message = "Access key is required" });
                }

                // Hash the provided access key

                // Check if access key matches any non-admin user password
                var isValid = await _db.User
                    .AnyAsync(u => u.Password == model.AccessKey && !u.IsAdminLogin);

                if (isValid)
                {
                    // Set session
                    HttpContext.Session.SetString("ProductAccessAuthenticated", "true");
                    HttpContext.Session.SetString("ProductAccessTime", DateTime.Now.ToString());
                    
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

        [HttpPost]
        public IActionResult ClearProductAccess()
        {
            HttpContext.Session.Remove("ProductAccessAuthenticated");
            HttpContext.Session.Remove("ProductAccessTime");
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetDispenserStatuses()
        {
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

            var hasPriceUpdateLogs = dispenserStatuses.Any(d => d.HasUpdateLog);

            return Json(new { 
                success = true, 
                dispenserStatuses = dispenserStatuses,
                hasPriceUpdateLogs = hasPriceUpdateLogs
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePrices([FromBody] List<UpdateProductPriceRequestDTO> priceUpdates)
        {
            try
            {
                if (priceUpdates == null || !priceUpdates.Any())
                {
                    return Json(new { success = false, message = "No price updates provided" });
                }

                var result = await _productService.UpdateProductPricesAsync(priceUpdates);

                if (result)
                {
                    return Json(new { success = true, message = "Prices updated successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update prices. Please check if dispensers are online." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return Json(new { success = false, message = "New password is required" });
                }

                if (request.NewPassword.Length < 4)
                {
                    return Json(new { success = false, message = "Password must be at least 4 characters long" });
                }

                // Get the first non-admin user (IsAdminLogin = false)
                var nonAdminUser = await _db.User
                    .FirstOrDefaultAsync(u => !u.IsAdminLogin);

                if (nonAdminUser == null)
                {
                    return Json(new { success = false, message = "No user found to update password" });
                }

                // Update the password
                nonAdminUser.Password = request.NewPassword;

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
