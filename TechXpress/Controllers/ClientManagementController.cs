using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using DAL.ViewModels;

namespace TechXpress.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClientManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClientManagementController> _logger;

        public ClientManagementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ClientManagementController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Orders)
                .Select(u => new ClientViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                    IsActive = u.IsActive,
                    RegistrationDate = u.RegistrationDate,
                    OrderCount = u.Orders.Count,
                    TotalSpent = u.Orders.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);

                return Json(new { 
                    success = true, 
                    message = user.IsActive ? "User activated successfully" : "User suspended successfully",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status");
                return Json(new { success = false, message = "Error updating user status" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Check if user has any orders
                var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == userId);
                if (hasOrders)
                {
                    return Json(new { success = false, message = "Cannot delete user with existing orders" });
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "User deleted successfully" });
                }

                return Json(new { success = false, message = "Error deleting user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return Json(new { success = false, message = "Error deleting user" });
            }
        }

        public async Task<IActionResult> UserDetails(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ClientDetailsViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                IsActive = user.IsActive,
                RegistrationDate = user.RegistrationDate,
                Orders = user.Orders.Select(o => new OrderSummaryViewModel
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    ItemsCount = o.OrderItems.Count
                }).ToList(),
                Reviews = user.Reviews.Select(r => new ReviewSummaryViewModel
                {
                    ProductName = r.Product.ProductName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }
    }
} 