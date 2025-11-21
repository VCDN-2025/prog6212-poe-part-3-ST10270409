using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMCS.Web.Models;
using CMCS.Web.Services;

namespace CMCS.Web.Controllers
{
    public sealed class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Landing / role-router
        [HttpGet]
        public IActionResult Index()
        {
            // If not logged in, show the normal landing page (Views/Home/Index.cshtml)
            if (User.Identity?.IsAuthenticated != true)
                return View();

            // Get role from cookie claims
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            switch (role)
            {
                case "HR":
                    // HR dashboard (Views/HR/Dashboard.cshtml)
                    return RedirectToAction("Dashboard", "HR");

                case "Manager":
                    // Manager approval screen
                    return RedirectToAction("Index", "Manager");

                case "Coordinator":
                    // Coordinator verification screen
                    return RedirectToAction("Index", "Coordinator");

                case "Lecturer":
                default:
                    // Lecturer’s own claims
                    return RedirectToAction("My", "Claims");
            }
        }

        [HttpGet]
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
