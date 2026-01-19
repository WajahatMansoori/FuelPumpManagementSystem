using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        // 🔐 Prevent overlapping cycles
        private static readonly SemaphoreSlim _cycleLock = new(1, 1);

        // 🔐 Prevent same dispenser running in parallel
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _dispenserLocks = new();

        // 📊 Track consecutive failures for each dispenser
        private static readonly ConcurrentDictionary<int, int> _dispenserFailureCount = new();

        public DispenserMonitoringSyncService(
            IServiceScopeFactory scopeFactory,
            ILogger<DispenserMonitoringSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

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
                if (!await _cycleLock.WaitAsync(0, stoppingToken))
                    continue;

                try
                {
                    await SyncAllDispensersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Dispenser sync cycle failed");
                    LogErrorToFile("SYNC_CYCLE", null, null, ex, "Dispenser sync cycle failed");
                }
                finally
                {
                    _cycleLock.Release();
                }
            }
        }

        private async Task SyncAllDispensersAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FPMSDbContext>();

            var dispensers = await db.Dispenser
                .Include(d => d.Nozzles)
                .Where(d => d.IsActive)
                .ToListAsync(token);

            _logger.LogInformation($"Found {dispensers.Count} active dispensers to sync. Online: {dispensers.Count(d => d.IsOnline)}, Offline: {dispensers.Count(d => !d.IsOnline)}");

            foreach (var dispenser in dispensers)
            {
                var hasEnabledNozzle = dispenser.Nozzles.Any(n => n.IsActive && n.IsEnable);
                if (!hasEnabledNozzle)
                    continue;

                _logger.LogDebug($"Processing Dispenser {dispenser.DispenserId} - IsOnline: {dispenser.IsOnline}, Endpoint: {dispenser.ApiEndPoint}");
                await SyncSingleDispenserAsync(dispenser, token);
            }
        }

        private async Task SyncSingleDispenserAsync(Dispenser dispenser, CancellationToken token)
        {
            var dispenserLock = _dispenserLocks.GetOrAdd(
                dispenser.DispenserId,
                _ => new SemaphoreSlim(1, 1));

            await dispenserLock.WaitAsync(token);

            bool apiCallSuccessful = false;

            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMilliseconds(800)
                };

                var url = $"{dispenser.ApiEndPoint}/status";
                var response = await httpClient.GetAsync(url, token);

                if (!response.IsSuccessStatusCode)
                {
                    // API call failed - increment failure counter
                    await HandleApiFailureAsync(dispenser);
                    return;
                }

                var json = await response.Content.ReadAsStringAsync(token);
                await ProcessDispenserStatusAsync(dispenser, json);
                
                // API call successful - reset failure counter and mark online
                apiCallSuccessful = true;
                await HandleApiSuccessAsync(dispenser);
            }
            catch (TaskCanceledException)
            {
                // hardware timeout – normal, but still count as failure
                await HandleApiFailureAsync(dispenser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing dispenser {dispenser.DispenserId}");

                LogErrorToFile("API_CALL", dispenser.DispenserId, null, ex,
                 $"Failed to call status API for Dispenser {dispenser.DispenserId} at {dispenser.ApiEndPoint}");
                
                // API call failed - increment failure counter
                await HandleApiFailureAsync(dispenser);
            }
            finally
            {
                dispenserLock.Release();
            }
        }

        private async Task ProcessDispenserStatusAsync(Dispenser dispenser, string json)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FPMSDbContext>();

            try
            {
                var statusData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (statusData == null || dispenser.IsLocked)
                    return;

                for (int nozzleNum = 1; nozzleNum <= 2; nozzleNum++)
                {
                    await UpdateDispenserLiveStatusAsync(db, dispenser, statusData, nozzleNum);
                    await ProcessNozzleTransaction(db, dispenser, statusData, nozzleNum);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing dispenser {dispenser.DispenserId}");

                LogErrorToFile("PROCESSING", dispenser.DispenserId, null, ex,
                    $"Error processing status data for Dispenser {dispenser.DispenserId}");
            }
        }

        private async Task UpdateDispenserLiveStatusAsync(
            FPMSDbContext db,
            Dispenser dispenser,
            Dictionary<string, JsonElement> statusData,
            int nozzleNum)
        {
            try
            {
                var prefix = $"N{nozzleNum}";

                // Extract hardware data
                TryGetDecimal(statusData, $"{prefix}_L", out var currentLiter);
                TryGetDecimal(statusData, $"{prefix}_A", out var currentAmount);
                TryGetDecimal(statusData, $"{prefix}_TL", out var hardwareTotalLiter);
                TryGetDecimal(statusData, $"{prefix}_TC", out var hardwareTotalCash);
                TryGetDecimal(statusData, $"{prefix}_UP", out var unitPrice);
                TryGetString(statusData, $"{prefix}_S", out var status);

                // Get the nozzle to check if it's active and enabled
                var nozzle = dispenser.Nozzles
                    .FirstOrDefault(n => n.NozzleId == nozzleNum && n.IsActive && n.IsEnable);

                if (nozzle == null)
                    return;

                // Get DispenserLiveStatus for this dispenser and nozzle where IsActive=1
                var liveStatus = await db.DispenserLiveStatus
                    .FirstOrDefaultAsync(ls => ls.DispenserId == dispenser.DispenserId 
                                            && ls.NozzleId == nozzleNum 
                                            && ls.IsActive);

                if (liveStatus == null)
                    return;

                // Map status string to readable format
                string nozzleStatus = status switch
                {
                    "I" => "IN",
                    "O" => "Out",
                    "F" => "FUELING",
                    _ => "UNKNOWN"
                };

                // Update DispenserLiveStatus with hardware data
                liveStatus.ProductTypeId = nozzle.ProductId;
                liveStatus.NozzleStatus = nozzleStatus;
                liveStatus.CurrentLiter = currentLiter;
                liveStatus.CurrentAmount = currentAmount;
                liveStatus.HardwareTotalLiter = hardwareTotalLiter;
                liveStatus.HardwareTotalCash = hardwareTotalCash;
                liveStatus.UnitPrice = unitPrice;
                liveStatus.IsOnline = dispenser.IsOnline;
                liveStatus.LastUpdatedAt = DateTime.Now;

                await db.SaveChangesAsync();

                _logger.LogDebug($"Updated LiveStatus | Dispenser:{dispenser.DispenserId} Nozzle:{nozzleNum} Status:{nozzleStatus} Liter:{currentLiter} Amount:{currentAmount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating live status for nozzle {nozzleNum} of dispenser {dispenser.DispenserId}");
                
                LogErrorToFile("LIVE_STATUS_UPDATE", dispenser.DispenserId, nozzleNum, ex,
                    $"Error updating DispenserLiveStatus for Nozzle {nozzleNum} of Dispenser {dispenser.DispenserId}");
            }
        }

        private async Task ProcessNozzleTransaction(
            FPMSDbContext db,
            Dispenser dispenser,
            Dictionary<string, JsonElement> statusData,
            int nozzleNum)
        {
            try
            {
                var prefix = $"N{nozzleNum}";

                if (!TryGetDecimal(statusData, $"{prefix}_L", out var liter)) return;
                if (!TryGetDecimal(statusData, $"{prefix}_A", out var amount)) return;
                if (!TryGetDecimal(statusData, $"{prefix}_TL", out var machineTotalLiter)) return;
                if (!TryGetDecimal(statusData, $"{prefix}_TC", out var machineTotalCash)) return;
                if (!TryGetDecimal(statusData, $"{prefix}_UP", out var unitPrice)) return;
                if (!TryGetString(statusData, $"{prefix}_S", out var status)) return;

                var nozzle = dispenser.Nozzles
                    .FirstOrDefault(n => n.NozzleId == nozzleNum && n.IsActive && n.IsEnable);

                if (nozzle == null)
                    return;

                if (status != "I" || liter <= 0 || amount <= 0)
                    return;

                var lastHardwareLiter = nozzle.LastHardwareTotalLiter ?? 0;
                var lastHardwareCash = nozzle.LastHardwareTotalCash ?? 0;

                var deltaLiter = machineTotalLiter - lastHardwareLiter;
                var deltaCash = machineTotalCash - lastHardwareCash;

                // Machine reset
                if (deltaLiter < 0 || deltaCash < 0)
                {
                    deltaLiter = machineTotalLiter;
                    deltaCash = machineTotalCash;
                }

                if (deltaLiter <= 0 || deltaCash <= 0)
                    return;

                db.Attach(nozzle);

                var transaction = new Transaction
                {
                    DispenserId = dispenser.DispenserId,
                    NozzleId = nozzleNum,
                    Liter = liter,
                    Amount = amount,
                    UnitPrice = unitPrice,
                    ProductTypeId = nozzle.ProductId ?? 0,
                    LastHardwareTotalCash= machineTotalCash,
                    LastHardwareTotalLiter= machineTotalLiter,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                db.Transaction.Add(transaction);

                nozzle.LastTotalLiter = (nozzle.LastTotalLiter ?? 0) + deltaLiter;
                nozzle.LastTotalCash = (nozzle.LastTotalCash ?? 0) + deltaCash;
                nozzle.LastHardwareTotalLiter = machineTotalLiter;
                nozzle.LastHardwareTotalCash = machineTotalCash;
                nozzle.UpdatedAt = DateTime.Now;

                await db.SaveChangesAsync();

                _logger.LogInformation(
                    $"Transaction saved | Dispenser:{dispenser.DispenserId} Nozzle:{nozzleNum} Liter:{liter} Amount:{amount}");
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
            if (!data.TryGetValue(key, out var element)) return false;

            try
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Number => element.TryGetDecimal(out value),
                    JsonValueKind.String => decimal.TryParse(element.GetString(), out value),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetString(Dictionary<string, JsonElement> data, string key, out string value)
        {
            value = string.Empty;
            if (!data.TryGetValue(key, out var element)) return false;

            try
            {
                value = element.GetString() ?? string.Empty;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task HandleApiFailureAsync(Dispenser dispenser)
        {
            // Increment failure counter
            var failureCount = _dispenserFailureCount.AddOrUpdate(
                dispenser.DispenserId,
                1, // Initial value if not exists
                (key, oldValue) => oldValue + 1); // Increment if exists

            _logger.LogWarning($"Dispenser {dispenser.DispenserId} API failure count: {failureCount}");

            // If 5 consecutive failures, mark dispenser as offline
            if (failureCount >= 5)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FPMSDbContext>();

                var dispenserEntity = await db.Dispenser
                    .FirstOrDefaultAsync(d => d.DispenserId == dispenser.DispenserId);

                if (dispenserEntity != null && dispenserEntity.IsOnline)
                {
                    dispenserEntity.IsOnline = false;
                    dispenserEntity.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync();

                    _logger.LogWarning($"Dispenser {dispenser.DispenserId} marked as OFFLINE after {failureCount} consecutive failures");
                }
            }
        }

        private async Task HandleApiSuccessAsync(Dispenser dispenser)
        {
            // Reset failure counter on success
            _dispenserFailureCount.AddOrUpdate(
                dispenser.DispenserId,
                0, // Initial value if not exists
                (key, oldValue) => 0); // Reset to 0

            // Mark dispenser as online if it was offline
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FPMSDbContext>();

            var dispenserEntity = await db.Dispenser
                .FirstOrDefaultAsync(d => d.DispenserId == dispenser.DispenserId);

            if (dispenserEntity != null && !dispenserEntity.IsOnline)
            {
                dispenserEntity.IsOnline = true;
                dispenserEntity.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync();

                _logger.LogInformation($"Dispenser {dispenser.DispenserId} marked as ONLINE after successful API call");
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
