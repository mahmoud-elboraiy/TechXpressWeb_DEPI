using DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TechXpress.ViewModels;
using System;
using Microsoft.Extensions.Logging;

namespace TechXpress.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    RegistrationDate = DateTime.UtcNow,
                    AccountStatus = "Active",
                    IsActive = true,
                    SuspensionReason = "N/A"
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    _logger.LogInformation($"User found: {user.Email}, Status: {user.AccountStatus}, IsActive: {user.IsActive}");

                    // Check if account is suspended
                    if (user.AccountStatus == "Suspended")
                    {
                        _logger.LogInformation($"Account is suspended. Checking suspension end date: {user.SuspensionEndDate}");
                        
                        // Check if suspension has expired
                        if (user.SuspensionEndDate.HasValue && user.SuspensionEndDate.Value < DateTime.UtcNow)
                        {
                            // Suspension has expired, reactivate account
                            user.AccountStatus = "Active";
                            user.IsActive = true;
                            user.SuspensionReason = null;
                            user.SuspensionEndDate = null;
                            await _userManager.UpdateAsync(user);
                            _logger.LogInformation("Account reactivated after suspension period ended");
                        }
                        else
                        {
                            // Account is still suspended
                            ModelState.AddModelError(string.Empty, 
                                $"Your account is suspended. Reason: {user.SuspensionReason}. " +
                                $"Please contact an administrator for assistance.");
                            return View(model);
                        }
                    }
                    else if (user.AccountStatus == "Banned")
                    {
                        _logger.LogInformation("Account is banned");
                        ModelState.AddModelError(string.Empty, 
                            "Your account has been banned. Please contact an administrator for assistance.");
                        return View(model);
                    }

                    // Check if account is active
                    if (!user.IsActive)
                    {
                        _logger.LogWarning("Account is not active");
                        ModelState.AddModelError(string.Empty, "Your account is not active. Please contact an administrator for assistance.");
                        return View(model);
                    }

                    // Attempt to sign in
                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
                    _logger.LogInformation($"Sign in result: {result.Succeeded}, Locked: {result.IsLockedOut}");

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User signed in successfully");
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("Index", "Home");
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Account is locked out");
                        return View("Lockout");
                    }
                    else
                    {
                        _logger.LogWarning("Invalid login attempt");
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }
                }
                else
                {
                    _logger.LogWarning($"User not found: {model.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
