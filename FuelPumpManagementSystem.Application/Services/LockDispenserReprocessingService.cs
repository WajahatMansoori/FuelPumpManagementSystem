using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    public class LockDispenserReprocessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LockDispenserReprocessingService> _logger;
        private readonly string _logFilePath;
        private static readonly object _logLock = new object();

        public LockDispenserReprocessingService(IServiceScopeFactory scopeFactory, ILogger<LockDispenserReprocessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Create daily log file
            var logFileName = $"LockDispenserReprocessing_{DateTime.Now:yyyy-MM-dd}.log";
            _logFilePath = Path.Combine(logDirectory, logFileName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(120));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ReprocessFailedLockActionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lock/Unlock reprocessing cycle failed");
                    LogErrorToFile("REPROCESS_CYCLE", null, ex, "Lock/Unlock reprocessing cycle failed");
                }
            }
        }

        private async Task ReprocessFailedLockActionsAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FPMSDbContext>();

            // Get failed lock/unlock action logs that haven't been resolved
            var failedLogs = await db.DispenserActionLog
                .Where(log => log.IsErrorOccured && !log.IsRecallAndResolve && log.IsActive)
                .ToListAsync(token);

            if (!failedLogs.Any())
            {
                _logger.LogDebug("No failed lock/unlock actions to reprocess");
                return;
            }

            _logger.LogInformation($"Found {failedLogs.Count} failed lock/unlock actions to reprocess");

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            foreach (var log in failedLogs)
            {
                await ReprocessSingleLockActionAsync(db, httpClient, log, token);
            }

            await db.SaveChangesAsync(token);
        }

        private async Task ReprocessSingleLockActionAsync(
            FPMSDbContext db,
            HttpClient httpClient,
            DispenserActionLog log,
            CancellationToken token)
        {
            try
            {
                // Get dispenser with ApiEndPoint
                var dispenser = await db.Dispenser
                    .FirstOrDefaultAsync(d => d.DispenserId == log.DispenserId && d.IsActive, token);

                if (dispenser == null)
                {
                    _logger.LogWarning($"Dispenser {log.DispenserId} not found or inactive");
                    return;
                }

                if (string.IsNullOrEmpty(dispenser.ApiEndPoint))
                {
                    _logger.LogWarning($"Dispenser {log.DispenserId} has no ApiEndPoint");
                    return;
                }

                if (string.IsNullOrEmpty(log.ApiRequest))
                {
                    _logger.LogWarning($"DispenserActionLog {log.DispenserActionLogId} has no ApiRequest");
                    return;
                }

                // Parse the ApiRequest to get lock status: { "LOCK": true/false }
                var lockPayload = JsonSerializer.Deserialize<Dictionary<string, bool>>(log.ApiRequest);
                if (lockPayload == null || !lockPayload.ContainsKey("LOCK"))
                {
                    _logger.LogWarning($"Failed to parse ApiRequest for DispenserActionLog {log.DispenserActionLogId}");
                    return;
                }

                bool isLocked = lockPayload["LOCK"];

                // Call hardware API
                var apiUrl = $"{dispenser.ApiEndPoint.TrimEnd('/')}/Lock";
                var jsonPayload = JsonSerializer.Serialize(lockPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Reprocessing lock/unlock action for Dispenser {dispenser.DispenserId} at {apiUrl} (Lock={isLocked})");

                var response = await httpClient.PostAsync(apiUrl, content, token);
                var responseContent = await response.Content.ReadAsStringAsync(token);

                if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Success - Mark as resolved
                    log.IsRecallAndResolve = true;
                    log.UpdatedAt = DateTime.Now;
                    log.IsErrorOccured = false;
                    log.ApiResponse = responseContent;
                    log.Message = isLocked 
                        ? "Dispenser locked successfully on retry" 
                        : "Dispenser unlocked successfully on retry";

                    _logger.LogInformation($"Successfully reprocessed lock/unlock action for Dispenser {dispenser.DispenserId}");

                    // Update Dispenser.IsLocked status
                    dispenser.IsLocked = isLocked;
                    dispenser.UpdatedAt = DateTime.Now;

                    _logger.LogDebug($"Updated Dispenser {dispenser.DispenserId} IsLocked to {isLocked}");
                }
                else
                {
                    // Still failed
                    _logger.LogWarning($"Lock/Unlock retry failed for Dispenser {dispenser.DispenserId}: {response.StatusCode}");
                    LogErrorToFile("RETRY_FAILED", log.DispenserId, null, 
                        $"Retry failed with status: {response.StatusCode}, Response: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error reprocessing lock/unlock action for Dispenser {log.DispenserId}");
                LogErrorToFile("HTTP_ERROR", log.DispenserId, ex, $"HTTP error during retry for Dispenser {log.DispenserId}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, $"Timeout reprocessing lock/unlock action for Dispenser {log.DispenserId}");
                LogErrorToFile("TIMEOUT", log.DispenserId, ex, $"Timeout during retry for Dispenser {log.DispenserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reprocessing lock/unlock action for Dispenser {log.DispenserId}");
                LogErrorToFile("GENERAL_ERROR", log.DispenserId, ex, $"Error during retry for Dispenser {log.DispenserId}");
            }
        }

        private void LogErrorToFile(string errorType, int? dispenserId, Exception ex, string customMessage)
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

                logEntry.AppendLine($"Message        : {customMessage}");

                if (ex != null)
                {
                    logEntry.AppendLine($"Exception Type : {ex.GetType().Name}");
                    logEntry.AppendLine($"Exception Msg  : {ex.Message}");

                    if (ex.InnerException != null)
                    {
                        logEntry.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                    }

                    logEntry.AppendLine($"Stack Trace    :");
                    logEntry.AppendLine(ex.StackTrace);
                }

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
                _logger.LogError(logEx, "Failed to write error to log file");
            }
        }
    }
}
