using Microsoft.AspNetCore.Mvc;
using FuelPumpManagementSystem.Application.Interfaces;
using FuelPumpManagementSystem.Web.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using AppInterfaces = FuelPumpManagementSystem.Application.Interfaces;
using WebModels = FuelPumpManagementSystem.Web.Models;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class FuelAnalyticsController : Controller
    {
        private readonly IFuelAnalyticsService _fuelAnalyticsService;

        public FuelAnalyticsController(IFuelAnalyticsService fuelAnalyticsService)
        {
            _fuelAnalyticsService = fuelAnalyticsService;
        }

        public async Task<IActionResult> Index()
        {
            // Get initial data for the dashboard
            var filters = new WebModels.FilterOptions
            {
                FromDate = DateTime.Now.AddMonths(-1),
                ToDate = DateTime.Now
            };
            
            var appViewModel = await _fuelAnalyticsService.GetAnalyticsDataAsync(new AppInterfaces.FilterOptions
            {
                FromDate = filters.FromDate,
                ToDate = filters.ToDate,
                DispenserIds = filters.DispenserIds,
                NozzleId = filters.NozzleId,
                ProductIds = filters.ProductIds,
                ViewType = filters.ViewType,
                FuelCategory = filters.FuelCategory,
                CombineCategories = filters.CombineCategories,
                Month = filters.Month,
                Year = filters.Year
            });
            
            // Convert Application layer model to Web layer model
            var webViewModel = new WebModels.FuelAnalyticsViewModel
            {
                Transactions = appViewModel.Transactions,
                Products = appViewModel.Products,
                FilterOptions = filters,
                WholePumpAnalytics = new WebModels.WholePumpAnalytics
                {
                    TotalSales = appViewModel.WholePumpAnalytics.TotalSales,
                    TotalLiters = appViewModel.WholePumpAnalytics.TotalLiters,
                    TotalTransactions = appViewModel.WholePumpAnalytics.TotalTransactions,
                    AverageTransaction = appViewModel.WholePumpAnalytics.AverageTransaction,
                    BestSellingFuel = appViewModel.WholePumpAnalytics.BestSellingFuel,
                    BusiestDispenser = appViewModel.WholePumpAnalytics.BusiestDispenser,
                    FuelTypeData = appViewModel.WholePumpAnalytics.FuelTypeData?.Select(f => new WebModels.FuelTypeData
                    {
                        ProductId = f.ProductId,
                        ProductName = f.ProductName,
                        TotalSales = f.TotalSales,
                        TotalLiters = f.TotalLiters,
                        TransactionCount = f.TransactionCount
                    }).ToList(),
                    DailyData = appViewModel.WholePumpAnalytics.DailyData?.Select(d => new WebModels.DailyData
                    {
                        Date = d.Date,
                        TotalSales = d.TotalSales,
                        TotalLiters = d.TotalLiters,
                        TransactionCount = d.TransactionCount
                    }).ToList(),
                    CategoryData = appViewModel.WholePumpAnalytics.CategoryData?.Select(c => new WebModels.CategoryData
                    {
                        ProductId = c.ProductId,
                        ProductName = c.ProductName,
                        TotalSales = c.TotalSales,
                        TotalLiters = c.TotalLiters,
                        TransactionCount = c.TransactionCount
                    }).ToList()
                },
                DispenserAnalytics = appViewModel.DispenserAnalytics?.Select(d => new WebModels.DispenserAnalytics
                {
                    DispenserId = d.DispenserId,
                    TotalSales = d.TotalSales,
                    TotalLiters = d.TotalLiters,
                    TransactionCount = d.TransactionCount,
                    AverageSale = d.AverageSale,
                    NozzleData = d.NozzleData?.Select(n => new WebModels.NozzleData
                    {
                        NozzleId = n.NozzleId,
                        TotalSales = n.TotalSales,
                        TotalLiters = n.TotalLiters,
                        TransactionCount = n.TransactionCount
                    }).ToList()
                }).ToList(),
                CategoryAnalytics = appViewModel.CategoryAnalytics?.Select(c => new WebModels.CategoryAnalytics
                {
                    ProductId = c.ProductId,
                    ProductName = c.ProductName,
                    TotalSales = c.TotalSales,
                    TotalLiters = c.TotalLiters,
                    TransactionCount = c.TransactionCount,
                    AveragePrice = c.AveragePrice,
                    MarketShare = c.MarketShare
                }).ToList(),
                MonthlyAnalytics = new WebModels.MonthlyAnalytics
                {
                    Month = appViewModel.MonthlyAnalytics.Month,
                    Year = appViewModel.MonthlyAnalytics.Year,
                    TotalSales = appViewModel.MonthlyAnalytics.TotalSales,
                    TotalLiters = appViewModel.MonthlyAnalytics.TotalLiters,
                    TransactionCount = appViewModel.MonthlyAnalytics.TransactionCount,
                    DailyData = appViewModel.MonthlyAnalytics.DailyData?.Select(d => new WebModels.DailyData
                    {
                        Date = d.Date,
                        TotalSales = d.TotalSales,
                        TotalLiters = d.TotalLiters,
                        TransactionCount = d.TransactionCount
                    }).ToList(),
                    YearlyData = appViewModel.MonthlyAnalytics.YearlyData?.Select(y => new WebModels.YearlyData
                    {
                        Year = y.Year,
                        TotalSales = y.TotalSales,
                        TotalLiters = y.TotalLiters,
                        TransactionCount = y.TransactionCount
                    }).ToList()
                }
            };
            
            return View(webViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAnalytics(WebModels.FilterOptions filters)
        {
            var appViewModel = await _fuelAnalyticsService.GetAnalyticsDataAsync(new AppInterfaces.FilterOptions
            {
                FromDate = filters.FromDate,
                ToDate = filters.ToDate,
                DispenserIds = filters.DispenserIds,
                NozzleId = filters.NozzleId,
                ProductIds = filters.ProductIds,
                ViewType = filters.ViewType,
                FuelCategory = filters.FuelCategory,
                CombineCategories = filters.CombineCategories,
                Month = filters.Month,
                Year = filters.Year
            });
            
            // Convert Application layer model to Web layer model
            var webViewModel = new WebModels.FuelAnalyticsViewModel
            {
                Transactions = appViewModel.Transactions,
                Products = appViewModel.Products,
                FilterOptions = filters,
                WholePumpAnalytics = new WebModels.WholePumpAnalytics
                {
                    TotalSales = appViewModel.WholePumpAnalytics.TotalSales,
                    TotalLiters = appViewModel.WholePumpAnalytics.TotalLiters,
                    TotalTransactions = appViewModel.WholePumpAnalytics.TotalTransactions,
                    AverageTransaction = appViewModel.WholePumpAnalytics.AverageTransaction,
                    BestSellingFuel = appViewModel.WholePumpAnalytics.BestSellingFuel,
                    BusiestDispenser = appViewModel.WholePumpAnalytics.BusiestDispenser,
                    FuelTypeData = appViewModel.WholePumpAnalytics.FuelTypeData?.Select(f => new WebModels.FuelTypeData
                    {
                        ProductId = f.ProductId,
                        ProductName = f.ProductName,
                        TotalSales = f.TotalSales,
                        TotalLiters = f.TotalLiters,
                        TransactionCount = f.TransactionCount
                    }).ToList(),
                    DailyData = appViewModel.WholePumpAnalytics.DailyData?.Select(d => new WebModels.DailyData
                    {
                        Date = d.Date,
                        TotalSales = d.TotalSales,
                        TotalLiters = d.TotalLiters,
                        TransactionCount = d.TransactionCount
                    }).ToList(),
                    CategoryData = appViewModel.WholePumpAnalytics.CategoryData?.Select(c => new WebModels.CategoryData
                    {
                        ProductId = c.ProductId,
                        ProductName = c.ProductName,
                        TotalSales = c.TotalSales,
                        TotalLiters = c.TotalLiters,
                        TransactionCount = c.TransactionCount
                    }).ToList()
                },
                DispenserAnalytics = appViewModel.DispenserAnalytics?.Select(d => new WebModels.DispenserAnalytics
                {
                    DispenserId = d.DispenserId,
                    TotalSales = d.TotalSales,
                    TotalLiters = d.TotalLiters,
                    TransactionCount = d.TransactionCount,
                    AverageSale = d.AverageSale,
                    NozzleData = d.NozzleData?.Select(n => new WebModels.NozzleData
                    {
                        NozzleId = n.NozzleId,
                        TotalSales = n.TotalSales,
                        TotalLiters = n.TotalLiters,
                        TransactionCount = n.TransactionCount
                    }).ToList()
                }).ToList(),
                CategoryAnalytics = appViewModel.CategoryAnalytics?.Select(c => new WebModels.CategoryAnalytics
                {
                    ProductId = c.ProductId,
                    ProductName = c.ProductName,
                    TotalSales = c.TotalSales,
                    TotalLiters = c.TotalLiters,
                    TransactionCount = c.TransactionCount,
                    AveragePrice = c.AveragePrice,
                    MarketShare = c.MarketShare
                }).ToList(),
                MonthlyAnalytics = new WebModels.MonthlyAnalytics
                {
                    Month = appViewModel.MonthlyAnalytics.Month,
                    Year = appViewModel.MonthlyAnalytics.Year,
                    TotalSales = appViewModel.MonthlyAnalytics.TotalSales,
                    TotalLiters = appViewModel.MonthlyAnalytics.TotalLiters,
                    TransactionCount = appViewModel.MonthlyAnalytics.TransactionCount,
                    DailyData = appViewModel.MonthlyAnalytics.DailyData?.Select(d => new WebModels.DailyData
                    {
                        Date = d.Date,
                        TotalSales = d.TotalSales,
                        TotalLiters = d.TotalLiters,
                        TransactionCount = d.TransactionCount
                    }).ToList(),
                    YearlyData = appViewModel.MonthlyAnalytics.YearlyData?.Select(y => new WebModels.YearlyData
                    {
                        Year = y.Year,
                        TotalSales = y.TotalSales,
                        TotalLiters = y.TotalLiters,
                        TransactionCount = y.TransactionCount
                    }).ToList()
                }
            };
            
            return PartialView("_AnalyticsData", webViewModel);
        }

        public async Task<IActionResult> ExportToPDF()
        {
            try
            {
                var filters = new WebModels.FilterOptions
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now
                };
                
                var pdfBytes = await _fuelAnalyticsService.GeneratePDFReportAsync(new AppInterfaces.FilterOptions
                {
                    FromDate = filters.FromDate,
                    ToDate = filters.ToDate,
                    DispenserIds = filters.DispenserIds,
                    NozzleId = filters.NozzleId,
                    ProductIds = filters.ProductIds,
                    ViewType = filters.ViewType,
                    FuelCategory = filters.FuelCategory,
                    CombineCategories = filters.CombineCategories,
                    Month = filters.Month,
                    Year = filters.Year
                });
                var fileName = $"Fuel_Analytics_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error exporting PDF: {ex.Message}");
            }
        }

        public async Task<IActionResult> ExportToCSV()
        {
            try
            {
                var filters = new WebModels.FilterOptions
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now
                };
                
                var csvBytes = await _fuelAnalyticsService.GenerateCSVReportAsync(new AppInterfaces.FilterOptions
                {
                    FromDate = filters.FromDate,
                    ToDate = filters.ToDate,
                    DispenserIds = filters.DispenserIds,
                    NozzleId = filters.NozzleId,
                    ProductIds = filters.ProductIds,
                    ViewType = filters.ViewType,
                    FuelCategory = filters.FuelCategory,
                    CombineCategories = filters.CombineCategories,
                    Month = filters.Month,
                    Year = filters.Year
                });
                var fileName = $"Fuel_Analytics_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                
                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error exporting CSV: {ex.Message}");
            }
        }
    }
}
