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

    // In-text reference: (Microsoft, 2024) ASP.NET Core MVC controllers and actions.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/controllers/actions
    [HttpGet]
    public IActionResult Login() => View();

    // In-text reference: (Microsoft, 2024) Cookie authentication and claims-based identity in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/security/authentication/cookie
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

            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(System.Security.Claims.ClaimTypes.Email, user.Email),
                new(System.Security.Claims.ClaimTypes.Name, user.Name),
                new(System.Security.Claims.ClaimTypes.Role, user.Role)
            };

            var identity = new System.Security.Claims.ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // In-text reference: (Microsoft, 2024) Using session state in ASP.NET Core.
            // URL: https://learn.microsoft.com/aspnet/core/fundamentals/app-state
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

    // In-text reference: (Microsoft, 2024) Sign-out and cookie invalidation in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/security/authentication/cookie
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData["ok"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    // In-text reference: (Microsoft, 2024) Role-based authorization in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/security/authorization/roles
    [Authorize(Roles = "HR")]
    public IActionResult ManageUsers()
    {
        var users = _userService.GetAllUsers();
        return View(users);
    }

    // In-text reference: (Microsoft, 2024) MVC model binding for GET and POST actions.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/models/model-binding
    [Authorize(Roles = "HR")]
    [HttpGet]
    public IActionResult CreateUser() => View();

    // In-text reference: (Microsoft, 2024) Model binding and validation in ASP.NET Core MVC.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/models/validation
    [Authorize(Roles = "HR")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CreateUser(User user)
    {
        try
        {
            // Manual validation for the fields HR actually maintains.
            if (string.IsNullOrWhiteSpace(user.Name) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Role))
            {
                ModelState.AddModelError(string.Empty, "Name, Email and Role are required.");
                return View(user);
            }

            if (user.Role == "Lecturer" && user.HourlyRate <= 0)
            {
                ModelState.AddModelError(nameof(user.HourlyRate),
                    "Hourly rate must be set for Lecturer accounts.");
                return View(user);
            }

            _userService.AddUser(user); // Service sets internal fields like CreatedAt, PasswordHash, etc.

            TempData["ok"] = $"User {user.Name} created successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to create user {Email}", user.Email);
            TempData["err"] = "Unable to create user. Please try again.";
            return View(user);
        }
    }

    // In-text reference: (Microsoft, 2024) Editing existing entities with EF Core in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/data/ef-mvc/crud
    [Authorize(Roles = "HR")]
    [HttpGet]
    public IActionResult EditUser(Guid id)
    {
        var user = _userService.GetUserById(id);
        if (user == null) return NotFound();
        return View(user);
    }

    // In-text reference: (Microsoft, 2024) ASP.NET Core MVC Model Binding.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/models/model-binding
    [Authorize(Roles = "HR")]
    [HttpGet]
    public IActionResult ResetPassword(Guid id)
    {
        var user = _userService.GetUserById(id);
        if (user == null) return NotFound();

        var vm = new ResetPasswordVm
        {
            UserId = user.Id,
            UserName = user.Name
        };

        return View(vm);
    }

    [Authorize(Roles = "HR")]
    [HttpPost, ValidateAntiForgeryToken]
    // In-text reference: (Microsoft, 2024) Password hashing with ASP.NET Core Identity.
    // URL: https://learn.microsoft.com/aspnet/core/security/authentication/identity
    public IActionResult ResetPassword(ResetPasswordVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = _userService.GetUserById(model.UserId);
        if (user == null) return NotFound();

        _userService.ResetPassword(user.Id, model.NewPassword);

        TempData["ok"] = $"Password for {user.Name} has been reset.";
        return RedirectToAction(nameof(ManageUsers));
    }

    // In-text reference: (Microsoft, 2024) Post-Redirect-Get pattern for form submissions.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/controllers/actions
    [Authorize(Roles = "HR")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EditUser(User user)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Name) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Role))
            {
                ModelState.AddModelError(string.Empty, "Name, Email and Role are required.");
                return View(user);
            }

            if (user.Role == "Lecturer" && user.HourlyRate <= 0)
            {
                ModelState.AddModelError(nameof(user.HourlyRate),
                    "Hourly rate must be set for Lecturer accounts.");
                return View(user);
            }

            _userService.UpdateUser(user);
            TempData["ok"] = $"User {user.Name} updated successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to update user {Id}", user.Id);
            TempData["err"] = "Unable to update user. Please try again.";
            return View(user);
        }
    }

    // In-text reference: (Microsoft, 2024) Implementing delete operations in ASP.NET Core MVC.
    // URL: https://learn.microsoft.com/aspnet/core/data/ef-mvc/crud
    [Authorize(Roles = "HR")]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteUser(Guid id)
    {
        try
        {
            _userService.DeleteUser(id);
            TempData["ok"] = "User deleted successfully.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to delete user {Id}", id);
            TempData["err"] = "Unable to delete user. Please try again.";
        }

        return RedirectToAction(nameof(ManageUsers));
    }

    // In-text reference: (Microsoft, 2024) Claims and Identity in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/security/authentication/identity#claims
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var vm = new ChangePasswordVm
        {
            UserId = Guid.Parse(userIdClaim.Value)
        };

        return View(vm);
    }

    // In-text reference: (Microsoft, 2024) Model validation in ASP.NET Core MVC.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/models/validation
    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var success = _userService.ChangePassword(
            model.UserId,
            model.CurrentPassword,
            model.NewPassword
        );

        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Current password is incorrect.");
            return View(model);
        }

        TempData["ok"] = "Your password has been updated successfully.";
        return RedirectToAction("Index", "Home");
    }

    // In-text reference: (Microsoft, 2024) Access denied handling for authorisation.
    // URL: https://learn.microsoft.com/aspnet/core/security/authorization/introduction
    [HttpGet]
    public IActionResult AccessDenied() => View();
}

/*
References

Microsoft. 2024. Controllers, actions, and routing in ASP.NET Core MVC. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/mvc/controllers/actions [Accessed 20 Nov 2025].

Microsoft. 2024. Cookie authentication in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/security/authentication/cookie [Accessed 20 Nov 2025].

Microsoft. 2024. Role-based authorization in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/security/authorization/roles [Accessed 20 Nov 2025].

Microsoft. 2024. Model binding and validation in ASP.NET Core MVC. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/mvc/models/validation [Accessed 20 Nov 2025].

Microsoft. 2024. Working with session state in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/fundamentals/app-state [Accessed 20 Nov 2025].

Microsoft. 2024. Implement CRUD operations with EF Core in an ASP.NET Core MVC app. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/data/ef-mvc/crud [Accessed 20 Nov 2025].

Microsoft. 2024. Introduction to authorization in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/security/authorization/introduction [Accessed 20 Nov 2025].

Microsoft. 2024. Password hashing and security in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/security/authentication/identity [Accessed 20 Nov 2025].
*/
