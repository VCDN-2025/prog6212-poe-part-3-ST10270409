using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<AccountController> _log;

    public AccountController(IUserService userService, ILogger<AccountController> log)
    {
        _userService = userService;
        _log = log;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string returnUrl = null!)
    {
        try
        {
            var user = _userService.Authenticate(email, password);
            if (user == null)
            {
                TempData["err"] = "Invalid email or password.";
                return View();
            }

            if (!user.IsActive)
            {
                TempData["err"] = "Account is deactivated.";
                return View();
            }

            // Use fully qualified name for System.Security.Claims.Claim
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role)
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.Name);

            TempData["ok"] = $"Welcome back, {user.Name}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Login failed for {Email}", email);
            TempData["err"] = "Login failed. Please try again.";
            return View();
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData["ok"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "HR")]
    public IActionResult ManageUsers()
    {
        var users = _userService.GetAllUsers();
        return View(users);
    }

    [Authorize(Roles = "HR")]
    [HttpGet]
    public IActionResult CreateUser() => View();

    [Authorize(Roles = "HR")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CreateUser(User user)
    {
        if (!ModelState.IsValid) return View(user);

        _userService.AddUser(user);
        TempData["ok"] = $"User {user.Name} created successfully.";
        return RedirectToAction(nameof(ManageUsers));
    }

    [Authorize(Roles = "HR")]
    [HttpGet]
    public IActionResult EditUser(Guid id)
    {
        var user = _userService.GetUserById(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [Authorize(Roles = "HR")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EditUser(User user)
    {
        if (!ModelState.IsValid) return View(user);

        _userService.UpdateUser(user);
        TempData["ok"] = $"User {user.Name} updated successfully.";
        return RedirectToAction(nameof(ManageUsers));
    }

    [Authorize(Roles = "HR")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteUser(Guid id)
    {
        _userService.DeleteUser(id);
        TempData["ok"] = "User deleted successfully.";
        return RedirectToAction(nameof(ManageUsers));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}