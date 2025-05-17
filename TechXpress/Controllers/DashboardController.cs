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

namespace TechXpress.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult AdminDash()
        {
            return View();
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
    }
}
