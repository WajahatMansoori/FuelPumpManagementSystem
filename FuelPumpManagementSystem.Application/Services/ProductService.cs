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
    public class ProductService : IProductService
    {
        private readonly FPMSDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductService(FPMSDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<ProductResponseDTO>> GetAllAsync()
        {
            return await _db.Product
                .Where(p => p.IsActive)
                .Select(p => new ProductResponseDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductColorCode = p.ProductColorCode
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateProductPricesAsync(List<UpdateProductPriceRequestDTO> priceUpdates)
        {
            // Get dispensers that are online and have at least one enabled nozzle
            var eligibleDispensers = await _db.Dispenser
                .Include(d => d.Nozzles)
                    .ThenInclude(n => n.Product)
                .Where(d => /*d.IsOnline && */d.IsActive && d.Nozzles.Any(n => n.IsEnable && n.IsActive))
                .ToListAsync();

            if (!eligibleDispensers.Any())
            {
                return false;
            }

            // Create price update batch
            var batch = new PriceUpdateBatch
            {
                BatchExecutionDate = DateTime.Now,
                TotalDispensor = eligibleDispensers.Count,
                SuccessCount = 0,
                FailedCount = 0,
                BatchStatusId = 1, // 1 = in progress
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _db.PriceUpdateBatch.Add(batch);
            await _db.SaveChangesAsync();

            int successCount = 0;
            int failedCount = 0;

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Call API for each eligible dispenser
            foreach (var dispenser in eligibleDispensers)
            {
                // Prepare price payload for this dispenser - all 6 products
                var pricePayload = new Dictionary<string, decimal>();
                for (int i = 1; i <= 6; i++)
                {
                    var productId = i;
                    var priceUpdate = priceUpdates.FirstOrDefault(p => p.ProductId == productId);
                    
                    if (priceUpdate != null)
                    {
                        // Use new price for updated products
                        pricePayload[$"P{i}"] = priceUpdate.NewPrice;
                    }
                    else
                    {
                        // Use current price from this dispenser's nozzles or default to 0
                        var nozzleWithProduct = dispenser.Nozzles
                            .FirstOrDefault(n => n.ProductId == productId && n.IsActive);
                        
                        pricePayload[$"P{i}"] = nozzleWithProduct?.CurrentProductPrice ?? 0;
                    }
                }

                var log = new PriceUpdateLog
                {
                    DispensorId = dispenser.DispenserId,
                    PriceUpdateBatchId = batch.PriceUpdateBatchId,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    IsRecallAndResolve = false
                };

                try
                {
                    var apiUrl = $"{dispenser.ApiEndPoint}/price";
                    var jsonPayload = JsonSerializer.Serialize(pricePayload);
                    log.ApiRequest = jsonPayload;

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(apiUrl, content);

                    var responseContent = await response.Content.ReadAsStringAsync();
                    log.ApiResponse = responseContent;

                    if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // Success - Update prices in DispenserNozzle table for this dispenser
                        log.IsErrorOccured = false;
                        log.Message = "Price updated successfully";
                        
                        foreach (var priceUpdate in priceUpdates)
                        {
                            var nozzle = dispenser.Nozzles
                                .FirstOrDefault(n => n.ProductId == priceUpdate.ProductId && n.IsActive);

                            if (nozzle != null)
                            {
                                nozzle.CurrentProductPrice = priceUpdate.NewPrice;
                                nozzle.UpdatedAt = DateTime.Now;
                            }
                            var product = _db.Product.FirstOrDefault(p => p.IsActive == true && p.ProductId == priceUpdate.ProductId);
                            if (product != null)
                            {
                                product.LastUpdatedPrice = priceUpdate.NewPrice;
                                _db.Product.Update(product);
                            }
                        }
                        
                        successCount++;
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
                                : "Price update failed";
                        }
                        catch
                        {
                            log.Message = $"Price update failed with status code: {response.StatusCode}";
                        }
                        
                        failedCount++;
                    }
                }
                catch (HttpRequestException ex)
                {
                    log.IsErrorOccured = true;
                    log.Message = $"HTTP Error: {ex.Message}";
                    log.ApiResponse = ex.ToString();
                    failedCount++;
                }
                catch (TaskCanceledException ex)
                {
                    log.IsErrorOccured = true;
                    log.Message = "Request timeout";
                    log.ApiResponse = ex.ToString();
                    failedCount++;
                }
                catch (Exception ex)
                {
                    log.IsErrorOccured = true;
                    log.Message = $"Error: {ex.Message}";
                    log.ApiResponse = ex.ToString();
                    failedCount++;
                }

                _db.PriceUpdateLog.Add(log);
                
            }

            // Update batch with final counts
            batch.SuccessCount = successCount;
            batch.FailedCount = failedCount;
            batch.BatchStatusId = 2; // 2 = completed
            batch.UpdatedAt = DateTime.Now;


            // Save all changes (batch, logs, and updated nozzle prices)
            await _db.SaveChangesAsync();

            // Return true to indicate processing completed (regardless of success/failure count)
            // This ensures UI updates immediately even when all dispensers fail
            return true;
        }
    }
}
