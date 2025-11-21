using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return View("Dashboard"); // Redirect to role-specific dashboard
        }
        return View();
    }

    [Authorize]
    public IActionResult Dashboard()
    {
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        ViewBag.UserRole = userRole;
        ViewBag.UserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        return View();
    }

    public IActionResult Privacy() => View();

    [Authorize(Roles = "HR,Manager")]
    public IActionResult Admin() => View();
}