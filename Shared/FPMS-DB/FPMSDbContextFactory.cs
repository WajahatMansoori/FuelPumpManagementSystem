using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace Shared.FPMS_DB
{
    public class FPMSDbContextFactory : IDesignTimeDbContextFactory<FPMSDbContext>
    {
        public FPMSDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FPMSDbContext>();

            // Set your connection string (absolute path recommended)
            //optionsBuilder.UseSqlite(@"Data Source=D:/Work/Freelance/Windsurf/FPMS/Shared/FuelPumpManagementSystem.db");
            optionsBuilder.UseSqlite("Data Source=FuelPumpManagementSystem.db");


            return new FPMSDbContext(optionsBuilder.Options);
        }
    }
}
