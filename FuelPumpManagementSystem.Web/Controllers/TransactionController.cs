using FuelPumpManagementSystem.Application.DTOs.Response;
using FuelPumpManagementSystem.Application.Interfaces;
using FuelPumpManagementSystem.Web.Models;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IO;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly IProductService _productService;

        public TransactionController(ITransactionService transactionService, IProductService productService)
        {
            _transactionService = transactionService;
            _productService = productService;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string[] dispenserIds, string nozzleId, string[] productIds, int page = 1, int pageSize = 10, string search = "")
        {
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"Received dispenserIds: {(dispenserIds != null ? string.Join(",", dispenserIds) : "null")}");
            System.Diagnostics.Debug.WriteLine($"Received nozzleId: {nozzleId}");
            System.Diagnostics.Debug.WriteLine($"Received productIds: {(productIds != null ? string.Join(",", productIds) : "null")}");
            System.Diagnostics.Debug.WriteLine($"Received search: {search}");

            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["NozzleId"] = nozzleId;
            ViewData["ProductIds"] = productIds;
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["Search"] = search;

            // Handle checkbox logic for dispensers
            if (dispenserIds != null && dispenserIds.Contains("ALL"))
            {
                // If ALL is selected, set to null to show all dispensers
                ViewData["DispenserIds"] = new string[] { "ALL" };
                dispenserIds = new string[] { "ALL" };
                System.Diagnostics.Debug.WriteLine("Setting dispenserIds to ALL");
            }
            else if (dispenserIds == null || dispenserIds.Length == 0)
            {
                // If no dispensers selected, default to ALL
                ViewData["DispenserIds"] = new string[] { "ALL" };
                dispenserIds = new string[] { "ALL" };
                System.Diagnostics.Debug.WriteLine("No dispenserIds selected, defaulting to ALL");
            }
            else
            {
                // Individual dispensers selected
                ViewData["DispenserIds"] = dispenserIds;
                System.Diagnostics.Debug.WriteLine($"Using individual dispenserIds: {string.Join(",", dispenserIds)}");
            }

            var transactions = await _transactionService.GetAllTransactionsAsync(fromDate, toDate, dispenserIds, nozzleId, productIds);
            var products = await _productService.GetAllAsync();
            var allDispenserNozzles = await _transactionService.GetAllDispenserNozzlesAsync();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                transactions = transactions.Where(t => 
                    t.TransactionId.ToString().Contains(search) ||
                    t.CreatedAt.ToString("yyyy-MM-dd HH:mm").Contains(search) ||
                    t.DispenserId.ToString().Contains(search) ||
                    t.NozzleId.ToString().Contains(search) ||
                    (products?.FirstOrDefault(p => p.ProductId == t.ProductTypeId)?.ProductName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    t.Liter.ToString().Contains(search) ||
                    t.UnitPrice.ToString().Contains(search) ||
                    t.Amount.ToString().Contains(search) ||
                    t.LastTotalLitre.ToString().Contains(search) ||
                    t.LastTotalCash.ToString().Contains(search)
                ).ToList();
            }

            // Calculate pagination info
            var totalRecords = transactions.Count;
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var skip = (page - 1) * pageSize;
            var pagedTransactions = transactions.Skip(skip).Take(pageSize).ToList();

            System.Diagnostics.Debug.WriteLine($"Found {transactions.Count} total transactions after search");
            System.Diagnostics.Debug.WriteLine($"Showing page {page} of {totalPages} with {pagedTransactions.Count} records");
            System.Diagnostics.Debug.WriteLine($"Found {allDispenserNozzles.Count} dispenser nozzles");

            var viewModel = new TransactionIndexViewModel
            {
                Transactions = pagedTransactions,
                Products = products
            };

            ViewData["AllDispenserNozzles"] = allDispenserNozzles;
            ViewData["TotalRecords"] = totalRecords;
            ViewData["TotalPages"] = totalPages;

            return View(viewModel);
        }

        public async Task<IActionResult> ExportToExcel(DateTime? fromDate, DateTime? toDate, string[] dispenserIds, string nozzleId, string[] productIds)
        {
            var transactions = await _transactionService.GetAllTransactionsAsync(fromDate, toDate, dispenserIds, nozzleId, productIds);
            var products = await _productService.GetAllAsync();

            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Transaction ID,Date Time,Dispenser,Nozzle,Fuel Type,Litres,Unit Price,Amount,Total Liter,Total Cash");
            
            // Data rows
            foreach (var t in transactions)
            {
                var product = products?.FirstOrDefault(p => p.ProductId == t.ProductTypeId);
                var fuelType = product?.ProductName ?? "Unknown";
                
                csv.AppendLine($"{t.TransactionId}," +
                             $"{t.CreatedAt:yyyy-MM-dd HH:mm}," +
                             $"{t.DispenserId}," +
                             $"{t.NozzleId}," +
                             $"\"{fuelType}\"," +
                             $"{t.Liter}," +
                             $"{t.UnitPrice}," +
                             $"{t.Amount}," +
                             $"{t.LastTotalLitre}," +
                             $"{t.LastTotalCash}");
            }

            var fileName = $"Transactions_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            
            return File(bytes, "text/csv", fileName);
        }

        public async Task<IActionResult> ExportToPdf(DateTime? fromDate, DateTime? toDate, string[] dispenserIds, string nozzleId, string[] productIds)
        {
            var transactions = await _transactionService.GetAllTransactionsAsync(fromDate, toDate, dispenserIds, nozzleId, productIds);
            var products = await _productService.GetAllAsync();

            // Create a simple PDF using basic PDF format
            var pdfContent = new StringBuilder();
            
            // PDF Header
            pdfContent.AppendLine("%PDF-1.4");
            pdfContent.AppendLine("1 0 obj");
            pdfContent.AppendLine("<< /Type /Catalog /Pages 2 0 R >>");
            pdfContent.AppendLine("endobj");
            
            // Pages object
            pdfContent.AppendLine("2 0 obj");
            pdfContent.AppendLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            pdfContent.AppendLine("endobj");
            
            // Page object
            pdfContent.AppendLine("3 0 obj");
            pdfContent.AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
            pdfContent.AppendLine("endobj");
            
            // Content stream
            var content = new StringBuilder();
            content.AppendLine("BT");
            content.AppendLine("/F1 12 Tf");
            content.AppendLine("50 700 Td");
            content.AppendLine("(FUEL PUMP TRANSACTIONS REPORT) Tj");
            content.AppendLine("0 -20 Td");
            
            // Date range
            if (fromDate.HasValue || toDate.HasValue)
            {
                content.AppendLine("(Date Range: ");
                if (fromDate.HasValue) content.AppendLine($"({fromDate.Value:yyyy-MM-dd}) Tj");
                if (fromDate.HasValue && toDate.HasValue) content.AppendLine("( to ) Tj");
                if (toDate.HasValue) content.AppendLine($"({toDate.Value:yyyy-MM-dd}) Tj");
                content.AppendLine(") Tj");
                content.AppendLine("0 -15 Td");
            }
            
            content.AppendLine("0 -30 Td");
            content.AppendLine("(Transaction ID  Date Time        Dispenser  Nozzle  Fuel Type    Litres    Unit Price  Amount    Total Liter  Total Cash) Tj");
            content.AppendLine("0 -15 Td");
            
            // Data rows
            foreach (var t in transactions.Take(50)) // Limit to 50 rows for simplicity
            {
                var product = products?.FirstOrDefault(p => p.ProductId == t.ProductTypeId);
                var fuelType = product?.ProductName ?? "Unknown";
                
                var line = $"{t.TransactionId,-15} {t.CreatedAt:yyyy-MM-dd HH:mm,-16} {t.DispenserId,-10} {t.NozzleId,-7} {fuelType,-12} {t.Liter,-10:F2} {t.UnitPrice,-11:F2} {t.Amount,-9:F2} {t.LastTotalLitre,-12:F2} {t.LastTotalCash,-11:F2}";
                content.AppendLine($"({line}) Tj");
                content.AppendLine("0 -12 Td");
            }
            
            content.AppendLine("ET");
            
            // Add content stream
            pdfContent.AppendLine("4 0 obj");
            pdfContent.AppendLine($"<< /Length {content.Length} >>");
            pdfContent.AppendLine("stream");
            pdfContent.Append(content.ToString());
            pdfContent.AppendLine("endstream");
            pdfContent.AppendLine("endobj");
            
            // Font object
            pdfContent.AppendLine("5 0 obj");
            pdfContent.AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
            pdfContent.AppendLine("endobj");
            
            // Cross-reference table
            pdfContent.AppendLine("xref");
            pdfContent.AppendLine("0 6");
            pdfContent.AppendLine("0000000000 65535 f ");
            pdfContent.AppendLine("0000000009 00000 n ");
            pdfContent.AppendLine("0000000058 00000 n ");
            pdfContent.AppendLine("0000000115 00000 n ");
            var contentStart = pdfContent.Length + content.Length + 100; // Approximate
            pdfContent.AppendLine($"{contentStart:D10} 00000 n ");
            var fontStart = contentStart + content.Length + 50; // Approximate
            pdfContent.AppendLine($"{fontStart:D10} 00000 n ");
            
            // Trailer
            pdfContent.AppendLine("trailer");
            pdfContent.AppendLine("<< /Size 6 /Root 1 0 R >>");
            pdfContent.AppendLine("startxref");
            pdfContent.AppendLine($"{pdfContent.Length}");
            pdfContent.AppendLine("%%EOF");

            var fileName = $"Transactions_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var bytes = Encoding.ASCII.GetBytes(pdfContent.ToString());
            
            return File(bytes, "application/pdf", fileName);
        }

        public async Task<IActionResult> PrintTransaction(int transactionId)
        {
            var allTransactions = await _transactionService.GetAllTransactionsAsync();
            var products = await _productService.GetAllAsync();
            var transaction = allTransactions.FirstOrDefault(t => t.TransactionId == transactionId);
            
            if (transaction == null)
            {
                return NotFound();
            }

            var product = products?.FirstOrDefault(p => p.ProductId == transaction.ProductTypeId);
            var fuelType = product?.ProductName ?? "Unknown";

            // Create a simple PDF for single transaction
            var pdfContent = new StringBuilder();
            
            // PDF Header
            pdfContent.AppendLine("%PDF-1.4");
            pdfContent.AppendLine("1 0 obj");
            pdfContent.AppendLine("<< /Type /Catalog /Pages 2 0 R >>");
            pdfContent.AppendLine("endobj");
            
            // Pages object
            pdfContent.AppendLine("2 0 obj");
            pdfContent.AppendLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            pdfContent.AppendLine("endobj");
            
            // Page object
            pdfContent.AppendLine("3 0 obj");
            pdfContent.AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
            pdfContent.AppendLine("endobj");
            
            // Content stream
            var content = new StringBuilder();
            content.AppendLine("BT");
            content.AppendLine("/F1 14 Tf");
            content.AppendLine("100 700 Td");
            content.AppendLine("(TRANSACTION RECEIPT) Tj");
            content.AppendLine("0 -40 Td");
            content.AppendLine("/F1 12 Tf");
            
            // Transaction details
            content.AppendLine("50 600 Td");
            content.AppendLine("(Transaction ID: ) Tj");
            content.AppendLine($"({transaction.TransactionId}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Date & Time: ) Tj");
            content.AppendLine($"({transaction.CreatedAt:yyyy-MM-dd HH:mm}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Dispenser: ) Tj");
            content.AppendLine($"({transaction.DispenserId}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Nozzle: ) Tj");
            content.AppendLine($"({transaction.NozzleId}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Fuel Type: ) Tj");
            content.AppendLine($"({fuelType}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Litres: ) Tj");
            content.AppendLine($"({transaction.Liter:F2}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Unit Price: ) Tj");
            content.AppendLine($"({transaction.UnitPrice:F2}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Total Amount: ) Tj");
            content.AppendLine($"({transaction.Amount:F2}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Total Liter: ) Tj");
            content.AppendLine($"({transaction.LastTotalLitre:F2}) Tj");
            content.AppendLine("0 -20 Td");
            content.AppendLine("(Total Cash: ) Tj");
            content.AppendLine($"({transaction.LastTotalCash:F2}) Tj");
            
            content.AppendLine("ET");
            
            // Add content stream
            pdfContent.AppendLine("4 0 obj");
            pdfContent.AppendLine($"<< /Length {content.Length} >>");
            pdfContent.AppendLine("stream");
            pdfContent.Append(content.ToString());
            pdfContent.AppendLine("endstream");
            pdfContent.AppendLine("endobj");
            
            // Font object
            pdfContent.AppendLine("5 0 obj");
            pdfContent.AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
            pdfContent.AppendLine("endobj");
            
            // Cross-reference table
            pdfContent.AppendLine("xref");
            pdfContent.AppendLine("0 6");
            pdfContent.AppendLine("0000000000 65535 f ");
            pdfContent.AppendLine("0000000009 00000 n ");
            pdfContent.AppendLine("0000000058 00000 n ");
            pdfContent.AppendLine("0000000115 00000 n ");
            var contentStart = pdfContent.Length + content.Length + 100;
            pdfContent.AppendLine($"{contentStart:D10} 00000 n ");
            var fontStart = contentStart + content.Length + 50;
            pdfContent.AppendLine($"{fontStart:D10} 00000 n ");
            
            // Trailer
            pdfContent.AppendLine("trailer");
            pdfContent.AppendLine("<< /Size 6 /Root 1 0 R >>");
            pdfContent.AppendLine("startxref");
            pdfContent.AppendLine($"{pdfContent.Length}");
            pdfContent.AppendLine("%%EOF");

            var fileName = $"Transaction_{transactionId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var bytes = Encoding.ASCII.GetBytes(pdfContent.ToString());
            
            return File(bytes, "application/pdf", fileName);
        }
    }
}
