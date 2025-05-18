using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using DAL.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace TechXpress.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> AdminDash()
        {
            try
            {
                // Get counts for dashboard cards
                var productCount = await _context.Products.CountAsync();
                var clientCount = await _context.Users.CountAsync();
                var orderCount = await _context.Orders.CountAsync();
                var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

                // Get recent orders
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new
                    {
                        o.Id,
                        CustomerName = o.User.FirstName + " " + o.User.LastName,
                        o.OrderDate,
                        o.Status,
                        o.TotalAmount
                    })
                    .ToListAsync();

                // Get top selling products
                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                    .GroupBy(oi => new { oi.ProductId, oi.Product.ProductName, oi.Product.Category.Name })
                    .Select(g => new
                    {
                        ProductName = g.Key.ProductName,
                        Category = g.Key.Name,
                        Sales = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.Price)
                    })
                    .OrderByDescending(x => x.Sales)
                    .Take(5)
                    .ToListAsync();

                ViewBag.ProductCount = productCount;
                ViewBag.ClientCount = clientCount;
                ViewBag.OrderCount = orderCount;
                ViewBag.TotalRevenue = totalRevenue.ToString("C");
                ViewBag.RecentOrders = recentOrders;
                ViewBag.TopProducts = topProducts;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return View();
            }
        }

        public IActionResult ManageClients()
        {
            return RedirectToAction("Index", "ClientManagement");
        }

        public async Task<IActionResult> ManageProduct()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(products);
        }

        // Category Management Actions
        public async Task<IActionResult> ManageCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Invalid category data: " + string.Join(", ", errors) });
            }

            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Category created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating category: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory([FromBody] Category category)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Invalid category data: " + string.Join(", ", errors) });
            }

            try
            {
                var existingCategory = await _context.Categories.FindAsync(category.Id);
                if (existingCategory == null)
                {
                    return Json(new { success = false, message = "Category not found" });
                }

                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Category updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating category: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Category not found" });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting category: " + ex.Message });
            }
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }

        // Product Management Actions
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            try
            {
                _logger.LogInformation("Starting product creation process");

                // Basic validation
                if (product == null)
                {
                    _logger.LogWarning("Product data is null");
                    return Json(new { success = false, message = "Product data is required" });
                }

                // Validate required fields
                var validationErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(product.ProductName))
                    validationErrors.Add("Product name is required");
                if (string.IsNullOrWhiteSpace(product.Description))
                    validationErrors.Add("Description is required");
                if (product.Price <= 0)
                    validationErrors.Add("Price must be greater than 0");
                if (product.StockQuantity < 0)
                    validationErrors.Add("Stock quantity cannot be negative");
                if (product.CategoryId <= 0)
                    validationErrors.Add("Category is required");

                if (validationErrors.Any())
                {
                    _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", validationErrors));
                    return Json(new { success = false, message = "Validation failed", errors = validationErrors });
                }

                // Set default image if none provided
                if (string.IsNullOrEmpty(product.Image))
                {
                    product.Image = "/images/no-image.png";
                }

                // Create new product
                var newProduct = new Product
                {
                    ProductName = product.ProductName.Trim(),
                    Description = product.Description.Trim(),
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    CategoryId = product.CategoryId,
                    Image = product.Image,
                    Specifications = product.Specifications, // Store specifications as JSON string
                };

                _logger.LogInformation("Adding product to database: {@Product}", newProduct);
                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product saved with ID: {ProductId}", newProduct.Id);

                return Json(new
                {
                    success = true,
                    message = "Product created successfully",
                    productId = newProduct.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {Message}", ex.Message);
                return Json(new
                {
                    success = false,
                    message = "Error creating product",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct([FromBody] Product product)
        {
            try
            {
                _logger.LogInformation("Starting product update process");

                // Basic validation
                if (product == null)
                {
                    _logger.LogWarning("Product data is null");
                    return Json(new { success = false, message = "Product data is required" });
                }

                // Validate required fields
                var validationErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(product.ProductName))
                    validationErrors.Add("Product name is required");
                if (string.IsNullOrWhiteSpace(product.Description))
                    validationErrors.Add("Description is required");
                if (product.Price <= 0)
                    validationErrors.Add("Price must be greater than 0");
                if (product.StockQuantity < 0)
                    validationErrors.Add("Stock quantity cannot be negative");
                if (product.CategoryId <= 0)
                    validationErrors.Add("Category is required");

                if (validationErrors.Any())
                {
                    _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", validationErrors));
                    return Json(new { success = false, message = "Validation failed", errors = validationErrors });
                }

                var existingProduct = await _context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Update basic properties
                existingProduct.ProductName = product.ProductName.Trim();
                existingProduct.Description = product.Description.Trim();
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.Image = product.Image ?? "/images/no-image.png";
                existingProduct.Specifications = product.Specifications;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                return Json(new { success = false, message = "Error updating product: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Product deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting product: " + ex.Message });
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        public async Task<IActionResult> EditProduct(int? id)
        {
            if (id == null)
            {
                return View(new Product
                {
                    Image = "/images/no-image.png" // Set default image for new products
                });
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();
            return Json(categories);
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                ViewBag.Message = "Search query is required";
                return View("ProductView", new List<Product>());
            }

            var products = await _context.Products
                .Where(p => p.ProductName.Contains(query))
                .Include(p => p.Category)
                .ToListAsync();

            return View("ProductView", products);
        }

        //Dashboard/GetAllOrderDetails
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDetailsDto>>> GetAllOrderDetails()
        {
            var orders = await _context.Orders
                     .Include(o => o.User)
                     .Include(o => o.OrderItems)
                         .ThenInclude(oi => oi.Product)
                     .Include(o => o.Payment)
                         .ThenInclude(p => p.Method)
                     .Include(o => o.Shipping)
                     .ToListAsync();
            var orderDtos = orders.Select(o => new OrderDetailsDto
            {
                Id = o.Id,
                UserId = o.UserId,
                UserFullName = $"{o.User.FirstName} {o.User.LastName}",
                UserEmail = o.User.Email,
                UserAddress = o.User.Address,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.ProductName,
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                    TotalItemsPrice = oi.TotalItemsPrice
                }).ToList(),
                Payment = o.Payment == null ? null : new PaymentDto
                {
                    PaymentMethodName = o.Payment.Method?.Name,
                    Amount = o.Payment.Amount,
                    TransactionDate = o.Payment.TransactionDate
                },
                Shipping = o.Shipping == null ? null : new ShippingDto
                {
                    ShippingState = o.Shipping.ShippingState,
                    TrackingNumber = o.Shipping.TrackingNumber,
                    EstimatedDeliveryDate = o.Shipping.EstimatedDeliveryDate,
                    ActualDeliveryDate = o.Shipping.ActualDeliveryDate
                }
            }).ToList();

            return View(orderDtos);
        }

        // Settings Management Actions
        public async Task<IActionResult> Settings()
        {
            var settings = await _context.StoreSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new StoreSettings
                {
                    StoreName = "TechXpress",
                    StoreEmail = "info@techxpress.com",
                    StorePhone = "+1 (555) 123-4567",
                    StoreAddress = "123 Tech Street, Silicon Valley, CA 94043, USA"
                };
                _context.StoreSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(StoreSettings model)
        {
            if (ModelState.IsValid)
            {
                var settings = await _context.StoreSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    _context.StoreSettings.Add(model);
                }
                else
                {
                    settings.StoreName = model.StoreName;
                    settings.StoreEmail = model.StoreEmail;
                    settings.StorePhone = model.StorePhone;
                    settings.StoreAddress = model.StoreAddress;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Settings updated successfully.";
                return RedirectToAction(nameof(Settings));
            }

            return View("Settings", model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserStatus(string userId, string status, string reason, DateTime? suspensionEndDate)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                user.AccountStatus = status;
                user.SuspensionReason = reason;
                user.SuspensionEndDate = suspensionEndDate;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "User status updated successfully." });
                }

                return Json(new { success = false, message = "Failed to update user status." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating user status." });
            }
        }
    }
}
