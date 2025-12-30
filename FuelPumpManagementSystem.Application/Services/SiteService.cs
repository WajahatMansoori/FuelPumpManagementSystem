using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.Interfaces;
using Shared.FPMS_DB;

namespace FuelPumpManagementSystem.Application.Services
{
    public class SiteService : ISiteService
    {
        private readonly FPMSDbContext _db;

        public SiteService(FPMSDbContext db)
        {
            _db = db;
        }

    }
}
