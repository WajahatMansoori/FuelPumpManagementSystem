using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.Interfaces;
using FuelPumpManagementSystem.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using Shared.Helpers;
using System.Linq;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class DispenserController : Controller
    {
        private readonly IDispenserService _dispenserService;
        private readonly IProductService _productService;
        private readonly ISiteService _siteService;
        private readonly FileUploadHelper _fileUploadHelper;
        private readonly FPMSDbContext _db;

        public DispenserController(
            IDispenserService dispenserService,
            IProductService productService,
            ISiteService siteService,
            FileUploadHelper fileUploadHelper,
            FPMSDbContext db)
        {
            _dispenserService = dispenserService;
            _productService = productService;
            _siteService = siteService;
            _fileUploadHelper = fileUploadHelper;
            _db = db;
        }
        public async Task<IActionResult> Index(int? id)
        {
            // Check if user is authenticated for dispenser management
            var isAuthenticated = HttpContext.Session.GetString("DispenserAccessAuthenticated");
            if (string.IsNullOrEmpty(isAuthenticated))
            {
                return View("AccessKeyLogin");
            }

            var dispensers = await _dispenserService.GetAllAsync();
            var products = await _productService.GetAllAsync();
            var siteDetail = await _siteService.GetAsync();
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
                Products = products,
                SiteDetail = siteDetail
            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> Configure(DispenserIndexViewModel model)
        {
            try
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
            catch (InvalidOperationException ex)
            {
                // Handle validation errors and business rule violations
                ViewBag.ErrorMessage = ex.Message;

                var dispensers = await _dispenserService.GetAllAsync();
                var products = await _productService.GetAllAsync();
                var siteDetail = await _siteService.GetAsync();

                var vm = new DispenserIndexViewModel
                {
                    Configure = model.Configure,
                    Dispensers = dispensers,
                    Products = products,
                    SiteDetail = siteDetail
                };

                return View("Index", vm);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                ViewBag.ErrorMessage = "An error occurred while saving the dispenser configuration. Please try again.";

                var dispensers = await _dispenserService.GetAllAsync();
                var products = await _productService.GetAllAsync();
                var siteDetail = await _siteService.GetAsync();

                var vm = new DispenserIndexViewModel
                {
                    Configure = model.Configure,
                    Dispensers = dispensers,
                    Products = products,
                    SiteDetail = siteDetail
                };

                return View("Index", vm);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSiteDetail(DispenserIndexViewModel model, IFormFile SiteLogo)
        {
            if (string.IsNullOrWhiteSpace(model.SiteDetail?.SiteName) ||
                string.IsNullOrWhiteSpace(model.SiteDetail?.SiteAddress) ||
                string.IsNullOrWhiteSpace(model.SiteDetail?.SitePhone))
            {
                ViewBag.SiteErrorMessage = "All fields are required except logo.";

                var dispensers = await _dispenserService.GetAllAsync();
                var products = await _productService.GetAllAsync();
                var siteDetail = await _siteService.GetAsync();

                var vm = new DispenserIndexViewModel
                {
                    Configure = new ConfigureDispenserRequestDTO(),
                    Dispensers = dispensers,
                    Products = products,
                    SiteDetail = siteDetail ?? model.SiteDetail
                };

                return View("Index", vm);
            }

            string? logoPath = model.SiteDetail?.SiteLogo;

            if (SiteLogo != null && SiteLogo.Length > 0)
            {
                logoPath = await _fileUploadHelper.UploadFileToLocalAsync(SiteLogo, "uploads");
            }

            var request = new SiteDetailRequestDTO
            {
                SiteName = model.SiteDetail?.SiteName,
                SiteAddress = model.SiteDetail?.SiteAddress,
                SitePhone = model.SiteDetail?.SitePhone,
                SiteLogo = logoPath
            };

            await _siteService.SaveAsync(request);

            return RedirectToAction(nameof(Index));
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

                // Check if access key matches any non-admin user password
                var isValid = await _db.User
                    .AnyAsync(u => u.Password == model.AccessKey && !u.IsAdminLogin);

                if (isValid)
                {
                    // Set session
                    HttpContext.Session.SetString("DispenserAccessAuthenticated", "true");
                    HttpContext.Session.SetString("DispenserAccessTime", DateTime.Now.ToString());
                    
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
        public IActionResult ClearDispenserAccess()
        {
            HttpContext.Session.Remove("DispenserAccessAuthenticated");
            HttpContext.Session.Remove("DispenserAccessTime");
            return Json(new { success = true });
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

        [HttpPost]
        public async Task<IActionResult> ValidateAdminAccessKey([FromBody] AdminAccessKeyRequestDTO request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccessKey))
                {
                    return Json(new { success = false, message = "Access key is required" });
                }

                // Check if access key matches any admin user password
                var isValid = await _db.User
                    .AnyAsync(u => u.Password == request.AccessKey && u.IsAdminLogin);

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

        [HttpPost]
        public async Task<IActionResult> LockUnlockDispenser([FromBody] LockUnlockDispenserRequestDTO request)
        {
            try
            {
                var success = await _dispenserService.LockUnlockDispenserAsync(request);

                if (success)
                {
                    var action = request.IsLocked ? "locked" : "unlocked";
                    return Json(new { success = true, message = $"Dispenser {action} successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update dispenser lock status" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
