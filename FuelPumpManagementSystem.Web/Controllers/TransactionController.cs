using Microsoft.AspNetCore.Mvc;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class TransactionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
