using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace TechXpress.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> viewProducts(string sortOrder)
        {
           
            var products = from p in _context.Products
                           .Include(p => p.Category)
                           .Include(p => p.Reviews)
                           select p;

            switch (sortOrder)
            {
                case "priceLowToHigh":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "priceHighToLow":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                //case "rating":
                //    products = products.OrderByDescending(p => p.Rating);
                //    break;
                case "newest":
                    products = products.OrderByDescending(p => p.Id);
                    break;
                default:
                    products = products.OrderBy(p => p.ProductName);
                    break;
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;
            return View(await products.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        [HttpGet]
        //products/search?name=phone
        public async Task<IActionResult> Search(string query)
        {
            List<Product> products = _context.Products
                .Include(c=>c.Category)
                .Where(p => p.ProductName.Contains(query.ToLower())).ToList();

            return View("viewProducts",products);
        }
    }
}
