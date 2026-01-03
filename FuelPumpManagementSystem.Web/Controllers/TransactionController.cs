using FuelPumpManagementSystem.Application.DTOs.Response;
using FuelPumpManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        //public async Task<IActionResult> Index()
        //{
        //    var transactions = await _transactionService.GetAllTransactionsAsync();
        //    return View(transactions);
        //}
        //public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        //{
        //    var transactions = await _transactionService.GetAllTransactionsAsync(fromDate, toDate);
        //    return View(transactions);
        //}

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

            var transactions = await _transactionService.GetAllTransactionsAsync(fromDate, toDate);
            return View(transactions);
        }

    }
}
