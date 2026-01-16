using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using Shared.FPMS_DB.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelPumpManagementSystem.Application.Services
{
    public class DatabaseHelperService
    {
        private readonly FPMSDbContext _db;

        public DatabaseHelperService(FPMSDbContext db)
        {
            _db = db;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                // Check and insert default User records
                if (!await _db.User.AnyAsync())
                {
                    var users = new List<User>
                    {
                        new User
                        {
                            Password = "admin0315",
                            IsAdminLogin = true,
                        },
                        new User
                        {
                            Password = "operator0315",
                            IsAdminLogin = false,
                        }
                    };

                    await _db.User.AddRangeAsync(users);
                    await _db.SaveChangesAsync();
                }

                // Check and insert default DispenserActionType records
                if (!await _db.DispenserActionType.AnyAsync())
                {
                    var actionTypes = new List<DispenserActionType>
                    {
                        new DispenserActionType
                        {
                            DispenserActionTypeName = "Locked",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true
                        },
                        new DispenserActionType
                        {
                            DispenserActionTypeName = "UnLocked",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true
                        }
                    };

                    await _db.DispenserActionType.AddRangeAsync(actionTypes);
                    await _db.SaveChangesAsync();
                }

                // Check and insert default Product records
                if (!await _db.Product.AnyAsync())
                {
                    var products = new List<Product>
                    {
                        new Product
                        {
                            ProductName = "Petrol",
                            ProductColorCode = "#2e8b75",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true
                        },
                        new Product
                        {
                            ProductName = "Hi-Octane",
                            ProductColorCode = "#ba7827",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true
                        },
                        new Product
                        {
                            ProductName = "Diesel",
                            ProductColorCode = "#237ca8",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true
                        },
                        new Product
                        {
                            ProductName = "Spare 1",
                            ProductColorCode = "#2b24bd",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true
                        },
                        new Product
                        {
                            ProductName = "Spare 2",
                            ProductColorCode = "#2b24bd",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true
                        },
                        new Product
                        {
                            ProductName = "Spare 3",
                            ProductColorCode = "#2b24bd",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true
                        }
                    };

                    await _db.Product.AddRangeAsync(products);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log the error or handle it appropriately
                throw new Exception($"Error initializing database: {ex.Message}", ex);
            }
        }
    }
}
