using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Response;
using FuelPumpManagementSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;

namespace FuelPumpManagementSystem.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly FPMSDbContext _db;

        public ProductService(FPMSDbContext db)
        {
            _db = db;
        }
        public async Task<List<ProductResponseDTO>> GetAllAsync()
        {
            return await _db.Product
                .Where(p => p.IsActive)
                .Select(p => new ProductResponseDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductPrice = p.ProductPrice,
                    ProductColorCode = p.ProductColorCode
                })
                .ToListAsync();
        }
    }
}
