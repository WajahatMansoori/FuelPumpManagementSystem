using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.Interfaces;
using FuelPumpManagementSystem.Application.DTOs.Response;

namespace FuelPumpManagementSystem.Application.Services
{
    public class FuelAnalyticsService : IFuelAnalyticsService
    {
        private readonly ITransactionService _transactionService;
        private readonly IProductService _productService;
        private readonly IPDFGenerationService _pdfGenerationService;

        public FuelAnalyticsService(ITransactionService transactionService, IProductService productService, IPDFGenerationService pdfGenerationService)
        {
            _transactionService = transactionService;
            _productService = productService;
            _pdfGenerationService = pdfGenerationService;
        }

        public async Task<FuelAnalyticsViewModel> GetAnalyticsDataAsync(FilterOptions filters)
        {
            var transactions = await GetFilteredTransactionsAsync(filters);
            var products = await _productService.GetAllAsync();

            return new FuelAnalyticsViewModel
            {
                Transactions = transactions,
                Products = products,
                FilterOptions = filters,
                WholePumpAnalytics = await GetWholePumpAnalyticsAsync(filters),
                DispenserAnalytics = await GetDispenserAnalyticsAsync(filters),
                CategoryAnalytics = await GetCategoryAnalyticsAsync(filters),
                MonthlyAnalytics = await GetMonthlyAnalyticsAsync(filters)
            };
        }

        public async Task<byte[]> GeneratePDFReportAsync(FilterOptions filters)
        {
            var transactions = await GetFilteredTransactionsAsync(filters);
            var products = await _productService.GetAllAsync();
            return await _pdfGenerationService.GenerateFuelAnalyticsPDF(transactions, products);
        }

        public async Task<byte[]> GenerateCSVReportAsync(FilterOptions filters)
        {
            var transactions = await GetFilteredTransactionsAsync(filters);
            var products = await _productService.GetAllAsync();
            return GenerateCSV(transactions, products);
        }

        public async Task<WholePumpAnalytics> GetWholePumpAnalyticsAsync(FilterOptions filters)
        {
            var transactions = await GetFilteredTransactionsAsync(filters);
            var products = await _productService.GetAllAsync();

            return new WholePumpAnalytics
            {
                TotalSales = transactions.Sum(t => t.Amount),
                TotalLiters = transactions.Sum(t => t.Liter),
                TotalTransactions = transactions.Count,
                AverageTransaction = transactions.Any() ? transactions.Sum(t => t.Amount) / transactions.Count : 0,
                BestSellingFuel = GetBestSellingFuel(transactions, products),
                BusiestDispenser = GetBusiestDispenser(transactions),
                FuelTypeData = GetFuelTypeData(transactions, products),
                DailyData = GetDailyData(transactions),
                CategoryData = GetCategoryData(transactions, products)
            };
        }

        public async Task<List<DispenserAnalytics>> GetDispenserAnalyticsAsync(FilterOptions filters)
        {
            var transactions = await GetFilteredTransactionsAsync(filters);
            var products = await _productService.GetAllAsync();

            return transactions
                .GroupBy(t => t.DispenserId)
                .Select(g => new DispenserAnalytics
                {
                    DispenserId = g.Key,
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count(),
                    AverageSale = g.Any() ? g.Sum(t => t.Amount) / g.Count() : 0,
                    NozzleData = g.GroupBy(t => t.NozzleId)
                        .Select(n => new NozzleData
                        {
                            NozzleId = n.Key,
                            TotalSales = n.Sum(t => t.Amount),
                            TotalLiters = n.Sum(t => t.Liter),
                            TransactionCount = n.Count()
                        }).ToList()
                })
                .OrderBy(x => x.DispenserId)
                .ToList();
        }

        public async Task<List<CategoryAnalytics>> GetCategoryAnalyticsAsync(FilterOptions filters)
        {
            var transactions = await GetFilteredTransactionsAsync(filters);
            var products = await _productService.GetAllAsync();

            return transactions
                .GroupBy(t => t.ProductTypeId)
                .Select(g => new CategoryAnalytics
                {
                    ProductId = g.Key,
                    ProductName = products?.FirstOrDefault(p => p.ProductId == g.Key)?.ProductName ?? "Unknown",
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count(),
                    AveragePrice = g.Sum(t => t.Amount) / g.Sum(t => t.Liter),
                    MarketShare = 0 // Will be calculated
                })
                .ToList();
        }

        public async Task<MonthlyAnalytics> GetMonthlyAnalyticsAsync(FilterOptions filters)
        {
            var transactions = await GetFilteredTransactionsAsync(filters);

            return new MonthlyAnalytics
            {
                Month = filters.Month ?? DateTime.Now.Month,
                Year = filters.Year ?? DateTime.Now.Year,
                TotalSales = transactions.Sum(t => t.Amount),
                TotalLiters = transactions.Sum(t => t.Liter),
                TransactionCount = transactions.Count,
                DailyData = GetDailyData(transactions),
                YearlyData = GetYearlyData(transactions)
            };
        }

        private async Task<List<TransactionResponseDTO>> GetFilteredTransactionsAsync(FilterOptions filters)
        {
            // Only use default dates if no dates are provided at all
            // If dates are provided, use them exactly as specified
            DateTime fromDate, toDate;
            
            if (filters.FromDate.HasValue)
            {
                fromDate = filters.FromDate.Value;
            }
            else
            {
                fromDate = DateTime.Now.AddMonths(-1); // Only fallback if no date provided
            }
            
            if (filters.ToDate.HasValue)
            {
                toDate = filters.ToDate.Value;
            }
            else
            {
                toDate = DateTime.Now; // Only fallback if no date provided
            }
            
            // Ensure toDate includes the entire day (end of day)
            toDate = toDate.Date.AddDays(1).AddTicks(-1);
            
            var dispenserIds = filters.DispenserIds?.Contains("ALL") == true ? null : filters.DispenserIds;
            var productIds = filters.ProductIds?.Contains("ALL") == true ? null : filters.ProductIds;

            return await _transactionService.GetAllTransactionsAsync(fromDate, toDate, dispenserIds, null, productIds);
        }

        private string GetBestSellingFuel(List<TransactionResponseDTO> transactions, List<ProductResponseDTO> products)
        {
            var bestFuel = transactions
                .GroupBy(t => t.ProductTypeId)
                .OrderByDescending(g => g.Sum(x => x.Amount))
                .FirstOrDefault();

            return bestFuel != null ? 
                products?.FirstOrDefault(p => p.ProductId == bestFuel.Key)?.ProductName : "N/A";
        }

        private string GetBusiestDispenser(List<TransactionResponseDTO> transactions)
        {
            var busiest = transactions
                .GroupBy(t => t.DispenserId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return busiest?.Key.ToString() ?? "0";
        }

        private List<FuelTypeData> GetFuelTypeData(List<TransactionResponseDTO> transactions, List<ProductResponseDTO> products)
        {
            return transactions
                .GroupBy(t => t.ProductTypeId)
                .Select(g => new FuelTypeData
                {
                    ProductId = g.Key,
                    ProductName = products?.FirstOrDefault(p => p.ProductId == g.Key)?.ProductName ?? "Unknown",
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count()
                })
                .ToList();
        }

        private List<DailyData> GetDailyData(List<TransactionResponseDTO> transactions)
        {
            return transactions
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new DailyData
                {
                    Date = g.Key,
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();
        }

        private List<CategoryData> GetCategoryData(List<TransactionResponseDTO> transactions, List<ProductResponseDTO> products)
        {
            return transactions
                .GroupBy(t => t.ProductTypeId)
                .Select(g => new CategoryData
                {
                    ProductId = g.Key,
                    ProductName = products?.FirstOrDefault(p => p.ProductId == g.Key)?.ProductName ?? "Unknown",
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count()
                })
                .ToList();
        }

        private List<YearlyData> GetYearlyData(List<TransactionResponseDTO> transactions)
        {
            return transactions
                .GroupBy(t => t.CreatedAt.Year)
                .Select(g => new YearlyData
                {
                    Year = g.Key,
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ToList();
        }

        public async Task<List<FuelTypeAnalytics>> GetFuelTypeAnalyticsAsync(FilterOptions filters)
        {
            var transactions = await GetFilteredTransactionsAsync(filters);
            var products = await _productService.GetAllAsync();

            return transactions
                .GroupBy(t => t.ProductTypeId)
                .Select(g => new FuelTypeAnalytics
                {
                    ProductId = g.Key,
                    ProductName = products?.FirstOrDefault(p => p.ProductId == g.Key)?.ProductName ?? "Unknown",
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count(),
                    AveragePrice = g.Sum(t => t.Amount) / g.Sum(t => t.Liter)
                })
                .ToList();
        }

        private byte[] GenerateCSV(List<TransactionResponseDTO> transactions, List<ProductResponseDTO> products)
        {
            var csv = new System.Text.StringBuilder();
            
            // Add comprehensive data including analytics summary
            csv.AppendLine("FUEL ANALYTICS REPORT");
            csv.AppendLine($"Generated On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Total Transactions: {transactions.Count}");
            csv.AppendLine($"Total Sales: {transactions.Sum(t => t.Amount):F2}");
            csv.AppendLine($"Total Liters: {transactions.Sum(t => t.Liter):F2}");
            csv.AppendLine("");
            
            // Summary by Fuel Type
            csv.AppendLine("FUEL TYPE SUMMARY");
            csv.AppendLine("Fuel Type,Total Sales,Total Liters,Transaction Count,Average Price");
            var fuelTypeSummary = transactions
                .GroupBy(t => t.ProductTypeId)
                .Select(g => new {
                    ProductId = g.Key,
                    ProductName = products?.FirstOrDefault(p => p.ProductId == g.Key)?.ProductName ?? "Unknown",
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count(),
                    AveragePrice = g.Sum(t => t.Amount) / g.Sum(t => t.Liter)
                })
                .OrderByDescending(x => x.TotalSales);
                
            foreach (var fuel in fuelTypeSummary)
            {
                csv.AppendLine($"\"{fuel.ProductName}\",{fuel.TotalSales:F2},{fuel.TotalLiters:F2},{fuel.TransactionCount},{fuel.AveragePrice:F2}");
            }
            
            csv.AppendLine("");
            
            // Summary by Dispenser
            csv.AppendLine("DISPENSER SUMMARY");
            csv.AppendLine("Dispenser ID,Total Sales,Total Liters,Transaction Count,Average Sale");
            var dispenserSummary = transactions
                .GroupBy(t => t.DispenserId)
                .Select(g => new {
                    DispenserId = g.Key,
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count(),
                    AverageSale = g.Any() ? g.Sum(t => t.Amount) / g.Count() : 0
                })
                .OrderByDescending(x => x.TotalSales);
                
            foreach (var dispenser in dispenserSummary)
            {
                csv.AppendLine($"{dispenser.DispenserId},{dispenser.TotalSales:F2},{dispenser.TotalLiters:F2},{dispenser.TransactionCount},{dispenser.AverageSale:F2}");
            }
            
            csv.AppendLine("");
            
            // Daily Summary
            csv.AppendLine("DAILY SUMMARY");
            csv.AppendLine("Date,Total Sales,Total Liters,Transaction Count,Average Sale");
            var dailySummary = transactions
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new {
                    Date = g.Key,
                    TotalSales = g.Sum(t => t.Amount),
                    TotalLiters = g.Sum(t => t.Liter),
                    TransactionCount = g.Count(),
                    AverageSale = g.Any() ? g.Sum(t => t.Amount) / g.Count() : 0
                })
                .OrderBy(x => x.Date);
                
            foreach (var day in dailySummary)
            {
                csv.AppendLine($"\"{day.Date:yyyy-MM-dd}\",{day.TotalSales:F2},{day.TotalLiters:F2},{day.TransactionCount},{day.AverageSale:F2}");
            }
            
            csv.AppendLine("");
            
            // Detailed Transactions
            csv.AppendLine("DETAILED TRANSACTIONS");
            csv.AppendLine("Transaction ID,Date Time,Dispenser,Nozzle,Fuel Type,Liters,Unit Price,Amount,Product ID");
            
            foreach (var t in transactions.OrderByDescending(x => x.CreatedAt))
            {
                var productName = products?.FirstOrDefault(p => p.ProductId == t.ProductTypeId)?.ProductName ?? "Unknown";
                csv.AppendLine($"{t.TransactionId}," +
                             $"\"{t.CreatedAt:yyyy-MM-dd HH:mm:ss}\"," +
                             $"{t.DispenserId}," +
                             $"{t.NozzleId}," +
                             $"\"{productName}\"," +
                             $"{t.Liter:F2}," +
                             $"{t.UnitPrice:F2}," +
                             $"{t.Amount:F2}," +
                             $"{t.ProductTypeId}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}
