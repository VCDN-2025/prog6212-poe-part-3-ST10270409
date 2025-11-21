using Microsoft.AspNetCore.Mvc;

namespace CMCS.Web.Controllers
{
    public class DatabaseTestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
