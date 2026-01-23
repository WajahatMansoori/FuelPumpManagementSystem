using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;

namespace FuelPumpManagementSystem.Web.ViewComponents
{
    public class SiteLogoViewComponent : ViewComponent
    {
        private readonly FPMSDbContext _db;

        public SiteLogoViewComponent(FPMSDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var siteDetail = await _db.SiteDetail
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            var logoPath = siteDetail?.SiteLogo;
            
            return View("Default", logoPath);
        }
    }
}
