using Microsoft.AspNetCore.Mvc;

namespace FuelPumpManagementSystem.Web.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
