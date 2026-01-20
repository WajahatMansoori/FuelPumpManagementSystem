using Microsoft.AspNetCore.SignalR;
using FuelPumpManagementSystem.Web.Models;

namespace FuelPumpManagementSystem.Web.Hubs
{
    public class DashboardHub : Hub
    {
        public async Task SendDashboardUpdate(DashboardViewModel data)
        {
            await Clients.All.SendAsync("ReceiveDashboardUpdate", data);
        }

        public async Task SendDispenserUpdate(DispenserModel dispenser)
        {
            await Clients.All.SendAsync("ReceiveDispenserUpdate", dispenser);
        }

        public async Task SendStatsUpdate(StatsModel stats)
        {
            await Clients.All.SendAsync("ReceiveStatsUpdate", stats);
        }
    }
}
