using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        // 🔐 Prevent overlapping cycles
        private static readonly SemaphoreSlim _cycleLock = new(1, 1);

        // 🔐 Prevent same dispenser running in parallel
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _dispenserLocks = new();

        public DispenserMonitoringSyncService(
            IServiceScopeFactory scopeFactory,
            ILogger<DispenserMonitoringSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
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

            foreach (var dispenser in dispensers)
            {
                var hasEnabledNozzle = dispenser.Nozzles.Any(n => n.IsActive && n.IsEnable);
                if (!hasEnabledNozzle)
                    continue;

                await SyncSingleDispenserAsync(dispenser, token);
            }
        }

        private async Task SyncSingleDispenserAsync(Dispenser dispenser, CancellationToken token)
        {
            var dispenserLock = _dispenserLocks.GetOrAdd(
                dispenser.DispenserId,
                _ => new SemaphoreSlim(1, 1));

            await dispenserLock.WaitAsync(token);

            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMilliseconds(800)
                };

                var url = $"{dispenser.ApiEndPoint}/status";
                var response = await httpClient.GetAsync(url, token);

                if (!response.IsSuccessStatusCode)
                    return;

                var json = await response.Content.ReadAsStringAsync(token);
                await ProcessDispenserStatusAsync(dispenser, json);
            }
            catch (TaskCanceledException)
            {
                // hardware timeout – normal
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing dispenser {dispenser.DispenserId}");
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
                    await ProcessNozzleTransaction(db, dispenser, statusData, nozzleNum);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing dispenser {dispenser.DispenserId}");
            }
        }

        private async Task ProcessNozzleTransaction(
            FPMSDbContext db,
            Dispenser dispenser,
            Dictionary<string, JsonElement> statusData,
            int nozzleNum)
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
    }
}
