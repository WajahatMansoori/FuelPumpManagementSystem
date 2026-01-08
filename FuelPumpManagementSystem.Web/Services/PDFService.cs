using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuelPumpManagementSystem.Application.DTOs.Response;
using SelectPdf;

namespace FuelPumpManagementSystem.Web.Services
{
    public class PDFService
    {
        public byte[] GenerateFuelAnalyticsPDF(List<TransactionResponseDTO> transactions, List<ProductResponseDTO> products)
        {
            try
            {
                var htmlContent = GenerateHTMLContent(transactions, products);
                
                // Convert HTML to PDF using SelectPdf
                HtmlToPdf converter = new HtmlToPdf();
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                converter.Options.MarginTop = 20;
                converter.Options.MarginBottom = 20;
                converter.Options.MarginLeft = 15;
                converter.Options.MarginRight = 15;
                
                // Convert HTML to PDF
                PdfDocument doc = converter.ConvertHtmlString(htmlContent);
                
                // Save to memory stream
                using (var memoryStream = new MemoryStream())
                {
                    doc.Save(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                // Return error PDF
                var errorHtml = "<html><body><h1>Error Generating PDF</h1><p>" + ex.Message + "</p></body></html>";
                HtmlToPdf converter = new HtmlToPdf();
                PdfDocument doc = converter.ConvertHtmlString(errorHtml);
                
                using (var memoryStream = new MemoryStream())
                {
                    doc.Save(memoryStream);
                    return memoryStream.ToArray();
                }
            }
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
            html.AppendLine(".summary-cards { display: flex; justify-content: space-between; margin-bottom: 20px; }");
            html.AppendLine(".summary-card { border: 1px solid #ddd; padding: 15px; margin: 0 10px; text-align: center; min-width: 150px; }");
            html.AppendLine(".summary-card h3 { margin: 0 0 10px 0; color: #007bff; }");
            html.AppendLine(".summary-card p { margin: 0; font-size: 18px; font-weight: bold; }");
            html.AppendLine(".section-title { font-size: 20px; font-weight: bold; margin-bottom: 15px; color: #333; border-bottom: 1px solid #eee; padding-bottom: 5px; }");
            html.AppendLine(".data-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
            html.AppendLine(".data-table th, .data-table td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine(".data-table th { background-color: #f8f9fa; font-weight: bold; }");
            html.AppendLine(".breakdown-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
            html.AppendLine(".breakdown-table th, .breakdown-table td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
            html.AppendLine(".breakdown-table th { background-color: #007bff; color: white; }");
            html.AppendLine(".footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #666; }");
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
            html.AppendLine("<th>Dispenser</th>");
            html.AppendLine("<th>Total Liters</th>");
            html.AppendLine("<th>Total Sales</th>");
            html.AppendLine("<th>Transactions</th>");
            html.AppendLine("<th>Performance %</th>");
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
                    TransactionCount = g.Count()
                })
                .OrderBy(x => x.DispenserId);

            var maxSales = dispenserBreakdown.Any() ? dispenserBreakdown.Max(x => x.TotalSales) : 0;
            foreach (var dispenser in dispenserBreakdown)
            {
                var performance = maxSales > 0 ? (dispenser.TotalSales / maxSales) * 100 : 0;
                var avgDispenserSale = dispenser.TransactionCount > 0 ? dispenser.TotalSales / dispenser.TransactionCount : 0;

                html.AppendLine("<tr>");
                html.AppendLine("<td>Dispenser " + dispenser.DispenserId.ToString() + "</td>");
                html.AppendLine("<td>" + dispenser.TotalLiters.ToString("N2") + "</td>");
                html.AppendLine("<td>₨ " + dispenser.TotalSales.ToString("N2") + "</td>");
                html.AppendLine("<td>" + dispenser.TransactionCount.ToString("N0") + "</td>");
                html.AppendLine("<td>" + performance.ToString("F2") + "%</td>");
                html.AppendLine("<td>₨ " + avgDispenserSale.ToString("N2") + "</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");
            
            // Footer
            html.AppendLine("<div class='footer'>");
            html.AppendLine("<p><strong>Report Generated by Fuel Analytics System</strong></p>");
            html.AppendLine("<p>For internal use only | Confidential</p>");
            html.AppendLine("<p>Page 1 of 1</p>");
            html.AppendLine("</div>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }
    }
}
