using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Response;

namespace FuelPumpManagementSystem.Application.Interfaces
{
    // Define all model classes in Application layer
    public class FilterOptions
    {
        public DateTime? FromDate { get; set; } = null;
        public DateTime? ToDate { get; set; } = null;
        public string[]? DispenserIds { get; set; } = null;
        public string? NozzleId { get; set; } = null;
        public string[]? ProductIds { get; set; } = null;
        public string ViewType { get; set; } = "whole";
        public string FuelCategory { get; set; } = "all";
        public bool CombineCategories { get; set; } = true;
        public int? Month { get; set; } = null;
        public int? Year { get; set; } = null;
    }

    public class FuelAnalyticsViewModel
    {
        public List<TransactionResponseDTO> Transactions { get; set; }
        public List<ProductResponseDTO> Products { get; set; }
        public FilterOptions FilterOptions { get; set; }
        public WholePumpAnalytics WholePumpAnalytics { get; set; }
        public List<DispenserAnalytics> DispenserAnalytics { get; set; }
        public List<CategoryAnalytics> CategoryAnalytics { get; set; }
        public MonthlyAnalytics MonthlyAnalytics { get; set; }
    }

    public class WholePumpAnalytics
    {
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransaction { get; set; }
        public string BestSellingFuel { get; set; }
        public string BusiestDispenser { get; set; }
        public List<FuelTypeData> FuelTypeData { get; set; }
        public List<DailyData> DailyData { get; set; }
        public List<CategoryData> CategoryData { get; set; }
    }

    public class DispenserAnalytics
    {
        public int DispenserId { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageSale { get; set; }
        public List<NozzleData> NozzleData { get; set; }
    }

    public class CategoryAnalytics
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal MarketShare { get; set; }
    }

    public class MonthlyAnalytics
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
        public List<DailyData> DailyData { get; set; }
        public List<YearlyData> YearlyData { get; set; }
    }

    public class FuelTypeData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
    }

    public class DailyData
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
    }

    public class CategoryData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
    }

    public class YearlyData
    {
        public int Year { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
    }

    public class NozzleData
    {
        public int NozzleId { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
    }

    public class FuelTypeAnalytics
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public interface IFuelAnalyticsService
    {
        Task<FuelAnalyticsViewModel> GetAnalyticsDataAsync(FilterOptions filters);
        Task<byte[]> GeneratePDFReportAsync(FilterOptions filters);
        Task<byte[]> GenerateCSVReportAsync(FilterOptions filters);
        Task<List<FuelTypeAnalytics>> GetFuelTypeAnalyticsAsync(FilterOptions filters);
        Task<List<DispenserAnalytics>> GetDispenserAnalyticsAsync(FilterOptions filters);
        Task<List<CategoryAnalytics>> GetCategoryAnalyticsAsync(FilterOptions filters);
        Task<MonthlyAnalytics> GetMonthlyAnalyticsAsync(FilterOptions filters);
        Task<WholePumpAnalytics> GetWholePumpAnalyticsAsync(FilterOptions filters);
    }
}
