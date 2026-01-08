using FuelPumpManagementSystem.Application.DTOs.Response;
using System;
using System.Collections.Generic;

namespace FuelPumpManagementSystem.Web.Models
{
    public class FuelAnalyticsViewModel
    {
        public List<TransactionResponseDTO>? Transactions { get; set; }
        public List<ProductResponseDTO>? Products { get; set; }
        public FilterOptions? FilterOptions { get; set; }
        
        // Analytics Data
        public WholePumpAnalytics? WholePumpAnalytics { get; set; }
        public List<DispenserAnalytics>? DispenserAnalytics { get; set; }
        public List<CategoryAnalytics>? CategoryAnalytics { get; set; }
        public MonthlyAnalytics? MonthlyAnalytics { get; set; }
    }

    public class FilterOptions
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string[]? DispenserIds { get; set; }
        public string? NozzleId { get; set; }
        public string[]? ProductIds { get; set; }
        public string ViewType { get; set; } = "whole"; // whole, dispenser, category, comparison
        public string FuelCategory { get; set; } = "all"; // all, petrol, diesel, hi-octane
        public bool CombineCategories { get; set; } = true;
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class WholePumpAnalytics
    {
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransaction { get; set; }
        public string? BestSellingFuel { get; set; }
        public string? BusiestDispenser { get; set; }
        public List<FuelTypeData>? FuelTypeData { get; set; }
        public List<DailyData>? DailyData { get; set; }
        public List<CategoryData>? CategoryData { get; set; }
    }

    public class DispenserAnalytics
    {
        public int DispenserId { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageSale { get; set; }
        public List<NozzleData>? NozzleData { get; set; }
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
        public List<DailyData>? DailyData { get; set; }
        public List<YearlyData>? YearlyData { get; set; }
    }

    // Supporting Data Classes
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

    public class MonthlyData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public decimal TotalLiters { get; set; }
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal GrowthPercentage { get; set; }
    }

    public class NozzleData
    {
        public int NozzleId { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
    }

    public class DispenserData
    {
        public int DispenserId { get; set; }
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

    public class FuelTypeAnalytics
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalLiters { get; set; }
        public int TransactionCount { get; set; }
        public decimal AveragePrice { get; set; }
    }
}
