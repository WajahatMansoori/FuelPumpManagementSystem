using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Response;
using FuelPumpManagementSystem.Application.Interfaces;

namespace FuelPumpManagementSystem.Application.Services
{
    public interface IPDFGenerationService
    {
        Task<byte[]> GenerateFuelAnalyticsPDF(List<TransactionResponseDTO> transactions, List<ProductResponseDTO> products);
    }

    public class PDFGenerationService : IPDFGenerationService
    {
        public async Task<byte[]> GenerateFuelAnalyticsPDF(List<TransactionResponseDTO> transactions, List<ProductResponseDTO> products)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var htmlContent = GenerateHTMLContent(transactions, products);
                    
                    // Try to use SelectPdf if available, otherwise fallback to HTML
                    try
                    {
                        // Check if SelectPdf is available
                        var selectPdfAssembly = System.Reflection.Assembly.Load("Select.HtmlToPdf");
                        if (selectPdfAssembly != null)
                        {
                            // Use SelectPdf for PDF generation
                            return GeneratePDFWithSelectPdf(htmlContent);
                        }
                    }
                    catch
                    {
                        // SelectPdf not available, fallback to HTML
                    }
                    
                    // Fallback: Return HTML with proper headers
                    var htmlWithHeaders = GenerateHTMLWithPDFHeaders(htmlContent);
                    return Encoding.UTF8.GetBytes(htmlWithHeaders);
                }
                catch (Exception ex)
                {
                    // Fallback to HTML if PDF generation fails
                    var errorHtml = GenerateErrorHTML(ex.Message);
                    return Encoding.UTF8.GetBytes(errorHtml);
                }
            });
        }

        private byte[] GeneratePDFWithSelectPdf(string htmlContent)
        {
            // This will only be called if SelectPdf is available
            try
            {
                var selectPdfAssembly = System.Reflection.Assembly.Load("Select.HtmlToPdf");
                var htmlToPdfType = selectPdfAssembly.GetType("SelectPdf.HtmlToPdf");
                var htmlToPdf = Activator.CreateInstance(htmlToPdfType);
                
                // Set properties using reflection
                var optionsProperty = htmlToPdfType.GetProperty("Options");
                var options = optionsProperty.GetValue(htmlToPdf);
                
                var pageSizeType = selectPdfAssembly.GetType("SelectPdf.PdfPageSize");
                var pageOrientationType = selectPdfAssembly.GetType("SelectPdf.PdfPageOrientation");
                
                optionsProperty.PropertyType.GetProperty("PdfPageSize").SetValue(options, Enum.Parse(pageSizeType, "A4"));
                optionsProperty.PropertyType.GetProperty("PdfPageOrientation").SetValue(options, Enum.Parse(pageOrientationType, "Portrait"));
                optionsProperty.PropertyType.GetProperty("MarginTop").SetValue(options, 20);
                optionsProperty.PropertyType.GetProperty("MarginBottom").SetValue(options, 20);
                optionsProperty.PropertyType.GetProperty("MarginLeft").SetValue(options, 15);
                optionsProperty.PropertyType.GetProperty("MarginRight").SetValue(options, 15);
                
                // Convert HTML to PDF
                var convertMethod = htmlToPdfType.GetMethod("ConvertHtmlString", new[] { typeof(string) });
                var pdf = convertMethod.Invoke(htmlToPdf, new[] { htmlContent });
                
                using (var memoryStream = new MemoryStream())
                {
                    var saveMethod = pdf.GetType().GetMethod("Save", new[] { typeof(Stream) });
                    saveMethod.Invoke(pdf, new[] { memoryStream });
                    return memoryStream.ToArray();
                }
            }
            catch
            {
                // Fallback to HTML if SelectPdf fails
                var htmlWithHeaders = GenerateHTMLWithPDFHeaders(htmlContent);
                return Encoding.UTF8.GetBytes(htmlWithHeaders);
            }
        }

        private string GenerateErrorHTML(string errorMessage)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Fuel Analytics Report - Error</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
            html.AppendLine(".error-container { text-align: center; padding: 40px; background: #f8f9fa; border: 1px solid #ddd; border-radius: 8px; }");
            html.AppendLine(".error-title { color: #dc3545; font-size: 24px; margin-bottom: 20px; }");
            html.AppendLine(".error-message { color: #666; font-size: 16px; margin-bottom: 30px; }");
            html.AppendLine(".fallback-info { color: #28a745; font-size: 14px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class='error-container'>");
            html.AppendLine("<h1 class='error-title'>PDF Generation Error</h1>");
            html.AppendLine("<p class='error-message'>" + errorMessage + "</p>");
            html.AppendLine("<p class='fallback-info'>This file contains the report data in HTML format. You can save it as .html and open in any browser.</p>");
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private string GenerateHTMLWithPDFHeaders(string htmlContent)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Fuel Analytics Report</title>");
            html.AppendLine("<meta http-equiv='Content-Type' content='text/html; charset=utf-8'>");
            html.AppendLine("<style>");
            html.AppendLine("@page { size: A4; margin: 2cm; }");
            html.AppendLine("@media print { body { font-size: 12pt; } .no-print { display: none; } }");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; line-height: 1.4; }");
            html.AppendLine(".header { text-align: center; border-bottom: 2px solid #007bff; padding-bottom: 20px; margin-bottom: 30px; }");
            html.AppendLine(".header h1 { color: #007bff; margin-bottom: 10px; }");
            html.AppendLine(".header p { color: #666; margin: 5px 0; }");
            html.AppendLine(".summary-section { margin-bottom: 30px; page-break-inside: avoid; }");
            html.AppendLine(".summary-cards { display: flex; justify-content: space-between; margin-bottom: 20px; flex-wrap: wrap; }");
            html.AppendLine(".summary-card { border: 1px solid #ddd; padding: 15px; margin: 0 10px 10px 0; text-align: center; min-width: 150px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            html.AppendLine(".summary-card h3 { margin: 0 0 10px 0; color: #007bff; font-size: 14px; }");
            html.AppendLine(".summary-card p { margin: 0; font-size: 18px; font-weight: bold; }");
            html.AppendLine(".section-title { font-size: 20px; font-weight: bold; margin-bottom: 15px; color: #333; border-bottom: 1px solid #eee; padding-bottom: 5px; }");
            html.AppendLine(".data-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; page-break-inside: auto; }");
            html.AppendLine(".data-table th, .data-table td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine(".data-table th { background-color: #f8f9fa; font-weight: bold; }");
            html.AppendLine(".breakdown-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; page-break-inside: auto; }");
            html.AppendLine(".breakdown-table th, .breakdown-table td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
            html.AppendLine(".breakdown-table th { background-color: #007bff; color: white; }");
            html.AppendLine(".footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #666; }");
            html.AppendLine(".no-print { display: block; background: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; margin: 10px 0; border-radius: 4px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Add print instruction
            html.AppendLine("<div class='no-print'>");
            html.AppendLine("<strong>Print Instructions:</strong> Use Ctrl+P or Print button to save as PDF. This report is formatted for A4 printing.");
            html.AppendLine("</div>");
            
            // Add the original HTML content (without the head and body tags)
            var contentStart = htmlContent.IndexOf("<body>") + 6;
            var contentEnd = htmlContent.IndexOf("</body>");
            if (contentStart > 5 && contentEnd > contentStart)
            {
                html.AppendLine(htmlContent.Substring(contentStart, contentEnd - contentStart));
            }
            else
            {
                html.AppendLine(htmlContent);
            }
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private string GenerateHTMLContent(List<TransactionResponseDTO> transactions, List<ProductResponseDTO> products)
        {
            var html = new StringBuilder();
            
            // Start HTML document
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Fuel Analytics Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine(".header { text-align: center; border-bottom: 2px solid #007bff; padding-bottom: 20px; margin-bottom: 30px; }");
            html.AppendLine(".header h1 { color: #007bff; margin-bottom: 10px; }");
            html.AppendLine(".header p { color: #666; margin: 5px 0; }");
            html.AppendLine(".summary-section { margin-bottom: 30px; }");
            html.AppendLine(".summary-cards { display: flex; justify-content: space-between; margin-bottom: 20px; flex-wrap: wrap; }");
            html.AppendLine(".summary-card { border: 1px solid #ddd; padding: 15px; margin: 0 10px 10px 0; text-align: center; min-width: 150px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            html.AppendLine(".summary-card h3 { margin: 0 0 10px 0; color: #007bff; font-size: 14px; }");
            html.AppendLine(".summary-card p { margin: 0; font-size: 18px; font-weight: bold; }");
            html.AppendLine(".section-title { font-size: 20px; font-weight: bold; margin-bottom: 15px; color: #333; border-bottom: 1px solid #eee; padding-bottom: 5px; }");
            html.AppendLine(".data-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
            html.AppendLine(".data-table th, .data-table td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine(".data-table th { background-color: #f8f9fa; font-weight: bold; }");
            html.AppendLine(".breakdown-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
            html.AppendLine(".breakdown-table th, .breakdown-table td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
            html.AppendLine(".breakdown-table th { background-color: #007bff; color: white; }");
            html.AppendLine(".footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #666; }");
            html.AppendLine(".chart-placeholder { background: #f8f9fa; border: 1px solid #ddd; padding: 20px; text-align: center; margin: 20px 0; }");
            html.AppendLine("@media print { .summary-cards { flex-direction: row; flex-wrap: wrap; } .summary-card { flex: 1; min-width: 120px; } }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header Section
            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>FUEL ANALYTICS REPORT</h1>");
            html.AppendLine("<p><strong>RES Fueling Station</strong></p>");
            html.AppendLine("<p>Gulshan-e-Shamim, Block-9, Yaseenabad, Karachi</p>");
            html.AppendLine("<p>Tel: +92-330-8530186</p>");
            html.AppendLine("<p>Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</p>");
            html.AppendLine("</div>");
            
            // Executive Summary
            html.AppendLine("<div class='summary-section'>");
            html.AppendLine("<h2 class='section-title'>EXECUTIVE SUMMARY</h2>");
            html.AppendLine("<div class='summary-cards'>");
            html.AppendLine("<div class='summary-card'>");
            html.AppendLine("<h3>Total Sales</h3>");
            html.AppendLine("<p>₨ " + transactions.Sum(t => t.Amount).ToString("N2") + "</p>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-card'>");
            html.AppendLine("<h3>Total Liters</h3>");
            html.AppendLine("<p>" + transactions.Sum(t => t.Liter).ToString("N2") + " L</p>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-card'>");
            html.AppendLine("<h3>Transactions</h3>");
            html.AppendLine("<p>" + transactions.Count.ToString("N0") + "</p>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-card'>");
            html.AppendLine("<h3>Average Sale</h3>");
            var avgSale = transactions.Any() ? (transactions.Sum(t => t.Amount) / transactions.Count) : 0;
            html.AppendLine("<p>₨ " + avgSale.ToString("N2") + "</p>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-card'>");
            html.AppendLine("<h3>Best Selling Fuel</h3>");
            var bestFuel = transactions
                .GroupBy(t => t.ProductTypeId)
                .OrderByDescending(g => g.Sum(x => x.Amount))
                .FirstOrDefault();
            var bestFuelName = bestFuel != null ? 
                products?.FirstOrDefault(p => p.ProductId == bestFuel.Key)?.ProductName ?? "Unknown" : "N/A";
            html.AppendLine("<p>" + bestFuelName + "</p>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-card'>");
            html.AppendLine("<h3>Busiest Dispenser</h3>");
            var busiestDispenser = transactions
                .GroupBy(t => t.DispenserId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            html.AppendLine("<p>Dispenser " + (busiestDispenser?.Key.ToString() ?? "0") + "</p>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            
            // Fuel Type Breakdown
            html.AppendLine("<div class='summary-section'>");
            html.AppendLine("<h2 class='section-title'>FUEL TYPE ANALYSIS</h2>");
            html.AppendLine("<table class='breakdown-table'>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Fuel Type</th>");
            html.AppendLine("<th>Total Liters</th>");
            html.AppendLine("<th>Total Sales</th>");
            html.AppendLine("<th>Transactions</th>");
            html.AppendLine("<th>Market Share %</th>");
            html.AppendLine("<th>Average Price/L</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");
            
            var fuelBreakdown = transactions
                .GroupBy(t => t.ProductTypeId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = products?.FirstOrDefault(p => p.ProductId == g.Key)?.ProductName ?? "Unknown",
                    TotalLiters = g.Sum(t => t.Liter),
                    TotalSales = g.Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(x => x.TotalSales);

            var totalSales = transactions.Sum(t => t.Amount);
            foreach (var fuel in fuelBreakdown)
            {
                var marketShare = totalSales > 0 ? (fuel.TotalSales / totalSales) * 100 : 0;
                var avgPrice = fuel.TotalLiters > 0 ? fuel.TotalSales / fuel.TotalLiters : 0;

                html.AppendLine("<tr>");
                html.AppendLine("<td>" + fuel.ProductName + "</td>");
                html.AppendLine("<td>" + fuel.TotalLiters.ToString("N2") + "</td>");
                html.AppendLine("<td>₨ " + fuel.TotalSales.ToString("N2") + "</td>");
                html.AppendLine("<td>" + fuel.TransactionCount.ToString("N0") + "</td>");
                html.AppendLine("<td>" + marketShare.ToString("F2") + "%</td>");
                html.AppendLine("<td>₨ " + avgPrice.ToString("N2") + "</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");
            
            // Dispenser Performance
            html.AppendLine("<div class='summary-section'>");
            html.AppendLine("<h2 class='section-title'>DISPENSER PERFORMANCE</h2>");
            html.AppendLine("<table class='breakdown-table'>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Dispenser ID</th>");
            html.AppendLine("<th>Total Liters</th>");
            html.AppendLine("<th>Total Sales</th>");
            html.AppendLine("<th>Transactions</th>");
            html.AppendLine("<th>Average Sale</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");
            
            var dispenserBreakdown = transactions
                .GroupBy(t => t.DispenserId)
                .Select(g => new
                {
                    DispenserId = g.Key,
                    TotalLiters = g.Sum(t => t.Liter),
                    TotalSales = g.Sum(t => t.Amount),
                    TransactionCount = g.Count(),
                    AverageSale = g.Any() ? g.Sum(t => t.Amount) / g.Count() : 0
                })
                .OrderByDescending(x => x.TotalSales);

            foreach (var dispenser in dispenserBreakdown)
            {
                html.AppendLine("<tr>");
                html.AppendLine("<td>Dispenser " + dispenser.DispenserId + "</td>");
                html.AppendLine("<td>" + dispenser.TotalLiters.ToString("N2") + "</td>");
                html.AppendLine("<td>₨ " + dispenser.TotalSales.ToString("N2") + "</td>");
                html.AppendLine("<td>" + dispenser.TransactionCount.ToString("N0") + "</td>");
                html.AppendLine("<td>₨ " + dispenser.AverageSale.ToString("N2") + "</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");
            
            // Daily Performance
            html.AppendLine("<div class='summary-section'>");
            html.AppendLine("<h2 class='section-title'>DAILY PERFORMANCE</h2>");
            html.AppendLine("<table class='breakdown-table'>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Date</th>");
            html.AppendLine("<th>Total Liters</th>");
            html.AppendLine("<th>Total Sales</th>");
            html.AppendLine("<th>Transactions</th>");
            html.AppendLine("<th>Average Sale</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");
            
            var dailyBreakdown = transactions
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalLiters = g.Sum(t => t.Liter),
                    TotalSales = g.Sum(t => t.Amount),
                    TransactionCount = g.Count(),
                    AverageSale = g.Any() ? g.Sum(t => t.Amount) / g.Count() : 0
                })
                .OrderByDescending(x => x.Date)
                .Take(30); // Last 30 days

            foreach (var day in dailyBreakdown)
            {
                html.AppendLine("<tr>");
                html.AppendLine("<td>" + day.Date.ToString("yyyy-MM-dd") + "</td>");
                html.AppendLine("<td>" + day.TotalLiters.ToString("N2") + "</td>");
                html.AppendLine("<td>₨ " + day.TotalSales.ToString("N2") + "</td>");
                html.AppendLine("<td>" + day.TransactionCount.ToString("N0") + "</td>");
                html.AppendLine("<td>₨ " + day.AverageSale.ToString("N2") + "</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");
            
            // Detailed Transactions
            html.AppendLine("<div class='summary-section'>");
            html.AppendLine("<h2 class='section-title'>DETAILED TRANSACTIONS</h2>");
            html.AppendLine("<table class='data-table'>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Transaction ID</th>");
            html.AppendLine("<th>Date & Time</th>");
            html.AppendLine("<th>Dispenser</th>");
            html.AppendLine("<th>Nozzle</th>");
            html.AppendLine("<th>Fuel Type</th>");
            html.AppendLine("<th>Liters</th>");
            html.AppendLine("<th>Unit Price</th>");
            html.AppendLine("<th>Total Amount</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");
            
            foreach (var t in transactions.OrderByDescending(x => x.CreatedAt).Take(100)) // Last 100 transactions
            {
                var productName = products?.FirstOrDefault(p => p.ProductId == t.ProductTypeId)?.ProductName ?? "Unknown";
                html.AppendLine("<tr>");
                html.AppendLine("<td>" + t.TransactionId + "</td>");
                html.AppendLine("<td>" + t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + "</td>");
                html.AppendLine("<td>" + t.DispenserId + "</td>");
                html.AppendLine("<td>" + t.NozzleId + "</td>");
                html.AppendLine("<td>" + productName + "</td>");
                html.AppendLine("<td>" + t.Liter.ToString("N2") + "</td>");
                html.AppendLine("<td>₨ " + t.UnitPrice.ToString("N2") + "</td>");
                html.AppendLine("<td>₨ " + t.Amount.ToString("N2") + "</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");
            
            // Footer
            html.AppendLine("<div class='footer'>");
            html.AppendLine("<p><strong>Report Generated by Fuel Analytics System</strong></p>");
            html.AppendLine("<p>For internal use only | Confidential</p>");
            html.AppendLine("<p>Total Records: " + transactions.Count + " | Report Period: " + 
                (transactions.Any() ? transactions.Min(t => t.CreatedAt).ToString("yyyy-MM-dd") : "N/A") + 
                " to " + 
                (transactions.Any() ? transactions.Max(t => t.CreatedAt).ToString("yyyy-MM-dd") : "N/A") + "</p>");
            html.AppendLine("</div>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }
    }
}
