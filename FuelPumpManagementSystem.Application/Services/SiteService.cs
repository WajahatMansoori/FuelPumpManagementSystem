using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using Shared.FPMS_DB.Entities;

namespace FuelPumpManagementSystem.Application.Services
{
    public class SiteService : ISiteService
    {
        private readonly FPMSDbContext _db;

        public SiteService(FPMSDbContext db)
        {
            _db = db;
        }
        public async Task<SiteDetailRequestDTO?> GetAsync()
        {
            var entity = await _db.SiteDetail
                .AsNoTracking()
                .OrderByDescending(s => s.SiteDetailId)
                .FirstOrDefaultAsync();

            if (entity == null)
                return null;

            return new SiteDetailRequestDTO
            {
                SiteName = entity.SiteName,
                SiteAddress = entity.SiteAddress,
                SitePhone = entity.SitePhone,
                SiteLogo = entity.SiteLogo
            };
        }

        public async Task SaveAsync(SiteDetailRequestDTO request)
        {
            var entity = await _db.SiteDetail.FirstOrDefaultAsync();

            if (entity == null)
            {
                entity = new SiteDetail
                {
                    SiteName = request.SiteName,
                    SiteAddress = request.SiteAddress,
                    SitePhone = request.SitePhone,
                    SiteLogo = request.SiteLogo,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _db.SiteDetail.Add(entity);
            }
            else
            {
                entity.SiteName = request.SiteName;
                entity.SiteAddress = request.SiteAddress;
                entity.SitePhone = request.SitePhone;

                if (!string.IsNullOrWhiteSpace(request.SiteLogo))
                {
                    entity.SiteLogo = request.SiteLogo;
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
