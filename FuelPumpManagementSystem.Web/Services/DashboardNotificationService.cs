using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using FuelPumpManagementSystem.Web.Hubs;
using Shared.FPMS_DB;

namespace FuelPumpManagementSystem.Web.Services
{
    public class DashboardNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<DashboardNotificationService> _logger;

        public DashboardNotificationService(
            IServiceScopeFactory scopeFactory,
            IHubContext<DashboardHub> hubContext,
            ILogger<DashboardNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait 2 seconds before starting to allow app to fully initialize
            await Task.Delay(2000, stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await SendAllDispenserUpdates();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in dashboard notification service");
                }
            }
        }

        private async Task SendAllDispenserUpdates()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FPMSDbContext>();

            // Get all active dispensers (those with at least one enabled nozzle)
            var dispensers = await db.Dispenser
                .Include(d => d.Nozzles)
                .Where(d => d.Nozzles.Any(n => n.IsEnable))
                .ToListAsync();

            foreach (var dispenser in dispensers)
            {
                await NotifyDispenserUpdate(db, dispenser.DispenserId);
            }
        }

        private async Task NotifyDispenserUpdate(FPMSDbContext db, int dispenserId)
        {
            try
            {
                var dispenser = await db.Dispenser
                    .Include(d => d.Nozzles)
                    .FirstOrDefaultAsync(d => d.DispenserId == dispenserId);

                if (dispenser == null) return;

                var liveStatuses = await db.DispenserLiveStatus
                    .Where(ls => ls.DispenserId == dispenserId)
                    .ToListAsync();

                var nozzle1Config = dispenser.Nozzles.FirstOrDefault(n => n.NozzleId == 1);
                var nozzle2Config = dispenser.Nozzles.FirstOrDefault(n => n.NozzleId == 2);

                var nozzle1LiveStatus = liveStatuses.FirstOrDefault(ls => ls.NozzleId == 1);
                var nozzle2LiveStatus = liveStatuses.FirstOrDefault(ls => ls.NozzleId == 2);

                // Only send data for enabled nozzles
                object? nozzle1Data = null;
                object? nozzle2Data = null;

                if (nozzle1Config != null && nozzle1Config.IsEnable)
                {
                    nozzle1Data = new
                    {
                        liters = nozzle1LiveStatus?.CurrentLiter ?? 0,
                        price = nozzle1LiveStatus?.CurrentAmount ?? 0,
                        totalLiters = nozzle1LiveStatus?.HardwareTotalLiter ?? 0,
                        status = MapNozzleStatus(nozzle1LiveStatus?.NozzleStatus)
                    };
                    
                    _logger.LogInformation($"[SignalR] Dispenser {dispenserId} Nozzle 1 - Liters: {nozzle1LiveStatus?.CurrentLiter}, Price: {nozzle1LiveStatus?.CurrentAmount}, TotalLiters: {nozzle1LiveStatus?.HardwareTotalLiter}, Status: {nozzle1LiveStatus?.NozzleStatus}");
                }

                if (nozzle2Config != null && nozzle2Config.IsEnable)
                {
                    nozzle2Data = new
                    {
                        liters = nozzle2LiveStatus?.CurrentLiter ?? 0,
                        price = nozzle2LiveStatus?.CurrentAmount ?? 0,
                        totalLiters = nozzle2LiveStatus?.HardwareTotalLiter ?? 0,
                        status = MapNozzleStatus(nozzle2LiveStatus?.NozzleStatus)
                    };
                    
                    _logger.LogInformation($"[SignalR] Dispenser {dispenserId} Nozzle 2 - Liters: {nozzle2LiveStatus?.CurrentLiter}, Price: {nozzle2LiveStatus?.CurrentAmount}, TotalLiters: {nozzle2LiveStatus?.HardwareTotalLiter}, Status: {nozzle2LiveStatus?.NozzleStatus}");
                }

                await _hubContext.Clients.All.SendAsync("ReceiveDispenserUpdate", new
                {
                    dispenserId = dispenserId,
                    isOnline = dispenser.IsOnline,
                    isLocked = dispenser.IsLocked,
                    nozzle1 = nozzle1Data,
                    nozzle2 = nozzle2Data
                });
                
                _logger.LogDebug($"[SignalR] Sent update for Dispenser {dispenserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending SignalR notification for dispenser {dispenserId}");
            }
        }

        private string MapNozzleStatus(string? nozzleStatus)
        {
            return nozzleStatus switch
            {
                "IDLE" => "IN",
                "FUELING" => "FUELING",
                "Out" => "OUT",
                _ => "IN"
            };
        }
    }
}
