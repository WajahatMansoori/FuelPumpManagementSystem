using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Request;
using FuelPumpManagementSystem.Application.DTOs.Response;
using FuelPumpManagementSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using Shared.FPMS_DB.Entities;

namespace FuelPumpManagementSystem.Application.Services
{
    public class DispenserService: IDispenserService
    {
        private readonly FPMSDbContext _db;

        public DispenserService(FPMSDbContext db)
        {
            _db = db;
        }

        public async Task<List<DispenserResponseDTO>> GetAllAsync()
        {
            return await _db.Dispenser
                .Include(d => d.Nozzles)
                    .ThenInclude(n => n.Product)
                .Select(d => new DispenserResponseDTO
                {
                    DispenserId = d.DispenserId,
                    ApiEndPoint = d.ApiEndPoint,
                    Nozzle1Enabled = d.Nozzles.Any(n => n.NozzleId == 1 && n.IsEnable),
                    Nozzle2Enabled = d.Nozzles.Any(n => n.NozzleId == 2 && n.IsEnable),
                    Nozzle1ProductTypeId = d.Nozzles
                        .Where(n => n.NozzleId == 1)
                        .Select(n => (int?)n.ProductId)
                        .FirstOrDefault(),
                    Nozzle2ProductTypeId = d.Nozzles
                        .Where(n => n.NozzleId == 2)
                        .Select(n => (int?)n.ProductId)
                        .FirstOrDefault(),
                    Nozzle1ProductTypeName = d.Nozzles
                        .Where(n => n.NozzleId == 1)
                        .Select(n => n.Product.ProductName)
                        .FirstOrDefault(),
                    Nozzle2ProductTypeName = d.Nozzles
                        .Where(n => n.NozzleId == 2)
                        .Select(n => n.Product.ProductName)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task ConfigureDispenserAsync1(ConfigureDispenserRequestDTO request)
        {
            var dispenser = new Dispenser
            {
                ApiEndPoint = request.ApiEndPoint,
                IsOnline = true,
                IsLocked = false,
                Nozzles = new List<DispenserNozzle>()
            };

            // Add nozzle 1 if enabled
            if (request.IsNozzle1Enabled)
            {
                dispenser.Nozzles.Add(new DispenserNozzle
                {
                    NozzleId = 1,
                    ProductId = request.Nozzle1ProductTypeId ?? 0,
                    IsEnable = true
                });
            }

            // Add nozzle 2 if enabled
            if (request.IsNozzle2Enabled)
            {
                dispenser.Nozzles.Add(new DispenserNozzle
                {
                    NozzleId = 2,
                    ProductId = request.Nozzle2ProductTypeId ?? 0,
                    IsEnable = true
                });
            }

            // Add dispenser with its related nozzles
            _db.Dispenser.Add(dispenser);

            // Save changes (EF will insert parent first, then child)
            await _db.SaveChangesAsync();
        }


        public async Task ConfigureDispenserAsync(ConfigureDispenserRequestDTO request)
        {
            try
            {
                string apiEndPoint = "http://" + request.ApiEndPoint + "/";
                var dispenser = new Dispenser
                {
                    ApiEndPoint = apiEndPoint,
                    IsOnline = true,
                    IsLocked = false
                };

                _db.Dispenser.Add(dispenser);
                await _db.SaveChangesAsync();


                if (request.IsNozzle1Enabled)
                {
                    _db.DispenserNozzle.Add(new DispenserNozzle
                    {
                        DispenserId = dispenser.DispenserId,
                        NozzleId = 1,
                        ProductId = request.Nozzle1ProductTypeId.Value,
                        IsEnable = true
                    });
                }

                if (request.IsNozzle2Enabled)
                {
                    _db.DispenserNozzle.Add(new DispenserNozzle
                    {
                        DispenserId = dispenser.DispenserId,
                        NozzleId = 2,
                        ProductId = request.Nozzle2ProductTypeId.Value,
                        IsEnable = true
                    });
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
