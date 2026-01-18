using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.DTOs.Response;
using FuelPumpManagementSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using Shared.FPMS_DB.Entities;

namespace FuelPumpManagementSystem.Application.Services
{
    public class DispenserService: IDispenserService
    {
        private readonly FPMSDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        public DispenserService(FPMSDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<DispenserResponseDTO>> GetAllAsync()
        {
            try
            {
                return await _db.Dispenser
                .Include(d => d.Nozzles)
                    .ThenInclude(n => n.Product)
                .Select(d => new DispenserResponseDTO
                {
                    DispenserId = d.DispenserId,
                    ApiEndPoint = d.ApiEndPoint,
                    IsLocked = d.IsLocked,
                    Nozzle1Enabled = d.Nozzles.Any(n => n.NozzleId == 1 && n.IsEnable),
                    Nozzle2Enabled = d.Nozzles.Any(n => n.NozzleId == 2 && n.IsEnable),
                    Nozzle1ProductTypeId = d.Nozzles
                        .Where(n => n.NozzleId == 1)
                        .Select(n => (int?)n.ProductId)
                        .FirstOrDefault(),
                    Nozzle2ProductTypeId = d.Nozzles
                        .Where(n => n.NozzleId == 2)
                        .Select(n => (int?)n.ProductId)
                        .FirstOrDefault(),
                    Nozzle1ProductTypeName = d.Nozzles
                        .Where(n => n.NozzleId == 1)
                        .Select(n => n.Product.ProductName)
                        .FirstOrDefault(),
                    Nozzle2ProductTypeName = d.Nozzles
                        .Where(n => n.NozzleId == 2)
                        .Select(n => n.Product.ProductName)
                        .FirstOrDefault()
                })
                .ToListAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        public async Task ConfigureDispenserAsync1(ConfigureDispenserRequestDTO request)
        {
            var dispenser = new Dispenser
            {
                ApiEndPoint = request.ApiEndPoint,
                IsOnline = true,
                IsLocked = false,
                Nozzles = new List<DispenserNozzle>()
            };

            // Add nozzle 1 if enabled
            if (request.IsNozzle1Enabled)
            {
                dispenser.Nozzles.Add(new DispenserNozzle
                {
                    NozzleId = 1,
                    ProductId = request.Nozzle1ProductTypeId ?? 0,
                    IsEnable = true
                });
            }

            // Add nozzle 2 if enabled
            if (request.IsNozzle2Enabled)
            {
                dispenser.Nozzles.Add(new DispenserNozzle
                {
                    NozzleId = 2,
                    ProductId = request.Nozzle2ProductTypeId ?? 0,
                    IsEnable = true
                });
            }

            // Add dispenser with its related nozzles
            _db.Dispenser.Add(dispenser);

            // Save changes (EF will insert parent first, then child)
            await _db.SaveChangesAsync();
        }


        public async Task ConfigureDispenserAsync(ConfigureDispenserRequestDTO request)
        {
            // Validate input before any database operations
            var rawIp = request.ApiEndPoint?.Trim();
            if (string.IsNullOrWhiteSpace(rawIp))
            {
                throw new InvalidOperationException("Please enter a valid dispenser IP address before saving.");
            }

            // Validate Nozzle 1: If enabled, ProductType must be selected
            if (request.IsNozzle1Enabled && !request.Nozzle1ProductTypeId.HasValue)
            {
                throw new InvalidOperationException("Nozzle 1 is enabled. Please select a Product Type for Nozzle 1.");
            }

            // Validate Nozzle 2: If enabled, ProductType must be selected
            if (request.IsNozzle2Enabled && !request.Nozzle2ProductTypeId.HasValue)
            {
                throw new InvalidOperationException("Nozzle 2 is enabled. Please select a Product Type for Nozzle 2.");
            }

            // Require at least one enabled nozzle with a selected product
            bool hasNozzle1 = request.IsNozzle1Enabled && request.Nozzle1ProductTypeId.HasValue;
            bool hasNozzle2 = request.IsNozzle2Enabled && request.Nozzle2ProductTypeId.HasValue;
            if (!hasNozzle1 && !hasNozzle2)
            {
                throw new InvalidOperationException("Please configure at least one nozzle with an associated product before saving.");
            }

            string apiEndPoint = "http://" + rawIp + "/";

            // Ensure ApiEndPoint is unique per dispenser
            bool exists = await _db.Dispenser.AnyAsync(d => d.ApiEndPoint == apiEndPoint);
            if (exists)
            {
                throw new InvalidOperationException("Dispenser IP already exists. Please use a unique IP address.");
            }

            // Use transaction to ensure all-or-nothing database operation
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var dispenser = new Dispenser
                {
                    ApiEndPoint = apiEndPoint,
                    IsOnline = true,
                    IsLocked = false
                };

                _db.Dispenser.Add(dispenser);
                await _db.SaveChangesAsync();

                if (request.IsNozzle1Enabled)
                {
                    _db.DispenserNozzle.Add(new DispenserNozzle
                    {
                        DispenserId = dispenser.DispenserId,
                        NozzleId = 1,
                        ProductId = request.Nozzle1ProductTypeId.Value,
                        IsEnable = true
                    });
                }

                if (request.IsNozzle2Enabled)
                {
                    _db.DispenserNozzle.Add(new DispenserNozzle
                    {
                        DispenserId = dispenser.DispenserId,
                        NozzleId = 2,
                        ProductId = request.Nozzle2ProductTypeId.Value,
                        IsEnable = true
                    });
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateDispenserAsync(ConfigureDispenserRequestDTO request)
        {
            if (!request.DispenserId.HasValue)
            {
                throw new ArgumentException("DispenserId is required for update.");
            }

            // Validate Nozzle 1: If enabled, ProductType must be selected
            if (request.IsNozzle1Enabled && !request.Nozzle1ProductTypeId.HasValue)
            {
                throw new InvalidOperationException("Nozzle 1 is enabled. Please select a Product Type for Nozzle 1.");
            }

            // Validate Nozzle 2: If enabled, ProductType must be selected
            if (request.IsNozzle2Enabled && !request.Nozzle2ProductTypeId.HasValue)
            {
                throw new InvalidOperationException("Nozzle 2 is enabled. Please select a Product Type for Nozzle 2.");
            }

            var dispenser = await _db.Dispenser
                .Include(d => d.Nozzles)
                .FirstOrDefaultAsync(d => d.DispenserId == request.DispenserId.Value);

            if (dispenser == null)
            {
                throw new InvalidOperationException($"Dispenser with id {request.DispenserId.Value} not found.");
            }
            if(request.ApiEndPoint!=null && !string.IsNullOrEmpty(request.ApiEndPoint))
            {
                dispenser.ApiEndPoint = request.ApiEndPoint;
            }
            var nozzle1 = dispenser.Nozzles.FirstOrDefault(n => n.NozzleId == 1);
            var nozzle2 = dispenser.Nozzles.FirstOrDefault(n => n.NozzleId == 2);

            // Update nozzle 1
            if (request.IsNozzle1Enabled)
            {
                if (nozzle1 == null)
                {
                    // Get current price from Product table LastUpdatePrice
                    decimal? currentPrice = null;
                    if (request.Nozzle1ProductTypeId.HasValue)
                    {
                        var product = await _db.Product
                            .Where(p => p.ProductId == request.Nozzle1ProductTypeId.Value)
                            .Select(p => p.LastUpdatedPrice)
                            .FirstOrDefaultAsync();
                        
                        currentPrice = product; // Will be null if product not found or LastUpdatePrice is null
                    }

                    nozzle1 = new DispenserNozzle
                    {
                        DispenserId = dispenser.DispenserId,
                        NozzleId = 1,
                        IsEnable = true,
                        ProductId = request.Nozzle1ProductTypeId ?? 0,
                        CurrentProductPrice = currentPrice
                    };
                    _db.DispenserNozzle.Add(nozzle1);
                }
                else
                {
                    nozzle1.IsEnable = true;
                    nozzle1.UpdatedAt = DateTime.Now;
                    if (request.Nozzle1ProductTypeId.HasValue)
                    {
                        nozzle1.ProductId = request.Nozzle1ProductTypeId.Value;
                        
                        // Update current price from Product table LastUpdatePrice
                        var product = await _db.Product
                            .Where(p => p.ProductId == request.Nozzle1ProductTypeId.Value)
                            .Select(p => p.LastUpdatedPrice)
                            .FirstOrDefaultAsync();
                        
                        nozzle1.CurrentProductPrice = product; // Will be null if product not found or LastUpdatePrice is null
                    }
                }
            }
            else if (nozzle1 != null)
            {
                nozzle1.IsEnable = false;
                nozzle1.UpdatedAt = DateTime.Now;
            }

            // Update nozzle 2
            if (request.IsNozzle2Enabled)
            {
                if (nozzle2 == null)
                {
                    // Get current price from Product table LastUpdatePrice
                    decimal? currentPrice = null;
                    if (request.Nozzle2ProductTypeId.HasValue)
                    {
                        var product = await _db.Product
                            .Where(p => p.ProductId == request.Nozzle2ProductTypeId.Value)
                            .Select(p => p.LastUpdatedPrice)
                            .FirstOrDefaultAsync();
                        
                        currentPrice = product; // Will be null if product not found or LastUpdatePrice is null
                    }

                    nozzle2 = new DispenserNozzle
                    {
                        DispenserId = dispenser.DispenserId,
                        NozzleId = 2,
                        IsEnable = true,
                        ProductId = request.Nozzle2ProductTypeId ?? 0,
                        CurrentProductPrice = currentPrice
                    };
                    _db.DispenserNozzle.Add(nozzle2);
                }
                else
                {
                    nozzle2.IsEnable = true;
                    nozzle2.UpdatedAt = DateTime.Now;
                    if (request.Nozzle2ProductTypeId.HasValue)
                    {
                        nozzle2.ProductId = request.Nozzle2ProductTypeId.Value;
                        
                        // Update current price from Product table LastUpdatePrice
                        var product = await _db.Product
                            .Where(p => p.ProductId == request.Nozzle2ProductTypeId.Value)
                            .Select(p => p.LastUpdatedPrice)
                            .FirstOrDefaultAsync();
                        
                        nozzle2.CurrentProductPrice = product; // Will be null if product not found or LastUpdatePrice is null
                    }
                }
            }
            else if (nozzle2 != null)
            {
                nozzle2.IsEnable = false;
                nozzle2.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<bool> LockUnlockDispenserAsync(LockUnlockDispenserRequestDTO request)
        {
            var log = new DispenserActionLog
            {
                DispenserId = request.DispenserId,
                DispenserActionTypeId = request.IsLocked ? 1 : 2, // 1 = Lock, 2 = Unlock
                IsRecallAndResolve=false,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            try
            {
                // Build API URL: ApiEndPoint/Lock
                var apiUrl = $"{request.ApiEndPoint.TrimEnd('/')}/Lock";

                // Build request body: { "LOCK": true/false }
                var requestBody = new { LOCK = request.IsLocked };
                var jsonPayload = JsonSerializer.Serialize(requestBody);
                log.ApiRequest = jsonPayload;

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(apiUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();
                log.ApiResponse = responseContent;

                if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Success
                    log.IsErrorOccured = false;
                    log.Message = request.IsLocked ? "Dispenser locked successfully" : "Dispenser unlocked successfully";

                    // Update dispenser lock status in database
                    var dispenser = await _db.Dispenser.FindAsync(request.DispenserId);
                    if (dispenser != null)
                    {
                        dispenser.IsLocked = request.IsLocked;
                        dispenser.UpdatedAt = DateTime.Now;
                    }

                    _db.DispenserActionLog.Add(log);
                    await _db.SaveChangesAsync();

                    return true;
                }
                else
                {
                    // Failed
                    log.IsErrorOccured = true;

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
                        log.Message = errorResponse?.ContainsKey("error") == true
                            ? errorResponse["error"]
                            : $"Lock/Unlock failed with status code: {response.StatusCode}";
                    }
                    catch
                    {
                        log.Message = $"Lock/Unlock failed with status code: {response.StatusCode}";
                    }

                    _db.DispenserActionLog.Add(log);
                    await _db.SaveChangesAsync();

                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                log.IsErrorOccured = true;
                log.Message = $"HTTP Error: {ex.Message}";
                log.ApiResponse = ex.ToString();

                _db.DispenserActionLog.Add(log);
                await _db.SaveChangesAsync();

                return false;
            }
            catch (TaskCanceledException ex)
            {
                log.IsErrorOccured = true;
                log.Message = "Request timeout";
                log.ApiResponse = ex.ToString();

                _db.DispenserActionLog.Add(log);
                await _db.SaveChangesAsync();

                return false;
            }
            catch (Exception ex)
            {
                log.IsErrorOccured = true;
                log.Message = $"Error: {ex.Message}";
                log.ApiResponse = ex.ToString();

                _db.DispenserActionLog.Add(log);
                await _db.SaveChangesAsync();

                return false;
            }
        }
    }
}
