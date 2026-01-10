using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.FPMS_DB;
using Shared.FPMS_DB.Entities;

namespace FuelPumpManagementSystem.Application.Services
{
    public class DispenserMonitoringSyncService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DispenserMonitoringSyncService> _logger;
        private readonly string _logFilePath;
        private static readonly object _logLock = new object();

        public DispenserMonitoringSyncService(IServiceScopeFactory scopeFactory,ILogger<DispenserMonitoringSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            
            // Create logs directory if it doesn't exist
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // Create daily log file
            var logFileName = $"DispenserSync_{DateTime.Now:yyyy-MM-dd}.log";
            _logFilePath = Path.Combine(logDirectory, logFileName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await SyncAllDispensersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Dispenser sync cycle failed");
                    LogErrorToFile("SYNC_CYCLE", null, null, ex, "Dispenser sync cycle failed");
                }
            }
        }

        private async Task SyncAllDispensersAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FPMSDbContext>();

            var dispensers = await db.Dispenser
                .AsNoTracking()
                .Include(d => d.Nozzles)
                .Where(d => d.IsActive)
                .ToListAsync(token);

            // Fire and forget - don't wait for all to complete
            var tasks = dispensers.Select(d =>
                Task.Run(async () => await SyncSingleDispenserAsync(d, token), token));

            // Don't await - let them run in background
            _ = Task.WhenAll(tasks);
        }

        private async Task SyncSingleDispenserAsync(Dispenser dispenser, CancellationToken token)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMilliseconds(800)
                };

                var url = $"{dispenser.ApiEndPoint}/status";

                var response = await httpClient.GetAsync(url, token);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(token);
                    // Pass dispenser object directly to avoid extra DB query
                    await ProcessDispenserStatusAsync(dispenser, json);
                }
            }
            catch (TaskCanceledException)
            {
                // timeout – very common in hardware APIs
            }
            catch (Exception ex)
            {
                // log but DO NOT throw
                // failure of one dispenser should not affect others
                _logger.LogError(ex, $"Error syncing dispenser {dispenser.DispenserId}");
                LogErrorToFile("API_CALL", dispenser.DispenserId, null, ex, 
                    $"Failed to call status API for Dispenser {dispenser.DispenserId} at {dispenser.ApiEndPoint}");
            }
        }
        private async Task ProcessDispenserStatusAsync(Dispenser dispenser, string json)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FPMSDbContext>();

            try
            {
                // Parse hardware API response
                var statusData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (statusData == null) return;

                if (dispenser.IsLocked) return;

                // Process each nozzle (N1 and N2) synchronously to avoid async overhead
                for (int nozzleNum = 1; nozzleNum <= 2; nozzleNum++)
                {
                    ProcessNozzleTransaction(db, dispenser, statusData, nozzleNum);
                }

                // Only save if there are changes
                if (db.ChangeTracker.HasChanges())
                {
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing dispenser {dispenser.DispenserId} status");
                LogErrorToFile("PROCESSING", dispenser.DispenserId, null, ex, 
                    $"Error processing status data for Dispenser {dispenser.DispenserId}");
            }
        }

        private void ProcessNozzleTransaction(
            FPMSDbContext db, 
            Dispenser dispenser, 
            Dictionary<string, JsonElement> statusData, 
            int nozzleNum)
        {
            try
            {
                // Extract nozzle data from API response
                var nozzlePrefix = $"N{nozzleNum}";
                
                if (!TryGetDecimal(statusData, $"{nozzlePrefix}_L", out decimal liter)) return;
                if (!TryGetDecimal(statusData, $"{nozzlePrefix}_A", out decimal amount)) return;
                if (!TryGetDecimal(statusData, $"{nozzlePrefix}_TL", out decimal totalLiter)) return;
                if (!TryGetDecimal(statusData, $"{nozzlePrefix}_TC", out decimal totalCash)) return;
                if (!TryGetDecimal(statusData, $"{nozzlePrefix}_UP", out decimal unitPrice)) return;
                if (!TryGetString(statusData, $"{nozzlePrefix}_S", out string status)) return;

                // Get nozzle configuration from already loaded nozzles
                var nozzle = dispenser.Nozzles
                    .FirstOrDefault(n => n.NozzleId == nozzleNum && n.IsActive);

                if (nozzle == null || !nozzle.IsEnable) return;

                // Initialize LastTotalLiter if null
                decimal lastSavedTotalLiter = nozzle.LastTotalLiter ?? 0;
                decimal lastSavedTotalCash = nozzle.LastTotalCash ?? 0;

                // Check all conditions for valid transaction:
                // 1. Nozzle status is "I" (nozzle returned to holster)
                // 2. Current TotalLiter > LastSavedTotalLiter (totalizer increased)
                // 3. Liter > 0 AND Amount > 0 (valid transaction)
                // 4. Nozzle is enabled (already checked above)
                // 5. Dispenser is not locked (already checked above)
                
                bool isNozzleReturned = status == "I";
                bool totalizerIncreased = totalLiter > lastSavedTotalLiter;
                bool validTransaction = liter > 0 && amount > 0;

                if (isNozzleReturned && totalizerIncreased && validTransaction)
                {
                    // Attach nozzle to context for tracking
                    db.Attach(nozzle);

                    // Create transaction record
                    var transaction = new Transaction
                    {
                        DispenserId = dispenser.DispenserId,
                        NozzleId = nozzleNum,
                        Amount = amount,
                        Liter = liter,
                        UnitPrice = unitPrice,
                        ProductTypeId = nozzle.ProductId ?? 0,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    db.Transaction.Add(transaction);

                    // Update nozzle totalizers
                    nozzle.LastTotalLiter = totalLiter;
                    nozzle.LastTotalCash = totalCash;
                    nozzle.UpdatedAt = DateTime.Now;

                    _logger.LogInformation(
                        $"Transaction recorded: Dispenser {dispenser.DispenserId}, Nozzle {nozzleNum}, " +
                        $"Amount: {amount}, Liter: {liter}, UnitPrice: {unitPrice}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing nozzle {nozzleNum} for dispenser {dispenser.DispenserId}");
                LogErrorToFile("NOZZLE_PROCESSING", dispenser.DispenserId, nozzleNum, ex, 
                    $"Error processing Nozzle {nozzleNum} for Dispenser {dispenser.DispenserId}");
            }
        }

        private bool TryGetDecimal(Dictionary<string, JsonElement> data, string key, out decimal value)
        {
            value = 0;
            if (!data.ContainsKey(key)) return false;
            
            try
            {
                if (data[key].ValueKind == JsonValueKind.Number)
                {
                    value = data[key].GetDecimal();
                    return true;
                }
                else if (data[key].ValueKind == JsonValueKind.String)
                {
                    return decimal.TryParse(data[key].GetString(), out value);
                }
            }
            catch
            {
                return false;
            }
            
            return false;
        }

        private bool TryGetString(Dictionary<string, JsonElement> data, string key, out string value)
        {
            value = string.Empty;
            if (!data.ContainsKey(key)) return false;
            
            try
            {
                value = data[key].GetString() ?? string.Empty;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LogErrorToFile(string errorType, int? dispenserId, int? nozzleId, Exception ex, string customMessage)
        {
            try
            {
                var logEntry = new StringBuilder();
                logEntry.AppendLine("=".PadRight(80, '='));
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR OCCURRED");
                logEntry.AppendLine("=".PadRight(80, '='));
                logEntry.AppendLine($"Error Type     : {errorType}");
                
                if (dispenserId.HasValue)
                {
                    logEntry.AppendLine($"Dispenser ID   : {dispenserId.Value}");
                }
                
                if (nozzleId.HasValue)
                {
                    logEntry.AppendLine($"Nozzle Number  : {nozzleId.Value}");
                }
                
                logEntry.AppendLine($"Message        : {customMessage}");
                logEntry.AppendLine($"Exception Type : {ex.GetType().Name}");
                logEntry.AppendLine($"Exception Msg  : {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    logEntry.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                logEntry.AppendLine($"Stack Trace    :");
                logEntry.AppendLine(ex.StackTrace);
                logEntry.AppendLine("=".PadRight(80, '='));
                logEntry.AppendLine();

                // Thread-safe file writing
                lock (_logLock)
                {
                    File.AppendAllText(_logFilePath, logEntry.ToString());
                }
            }
            catch (Exception logEx)
            {
                // If file logging fails, at least log to console
                _logger.LogError(logEx, "Failed to write error to log file");
            }
        }





    }
}
