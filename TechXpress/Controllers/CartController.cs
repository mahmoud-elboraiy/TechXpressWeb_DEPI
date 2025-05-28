using DAL;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TechXpress.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Cart()
        {
           // int  CartId = 2;
            var cartItems = _db.CartItems
                //.Where(c => c.CartId == CartId)
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .ToList();
            return View(cartItems);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
          var product =_db.Products.FirstOrDefault(p=>p.Id == productId);
            if (product == null)
            {
                return RedirectToAction( "viewProducts","Products");
            }
            var cartItem = new CartItem
            {
                ProductId = product.Id,
                Quantity = 1,
                CartId = 2,
                Price = product.Price,
                TotalItemsPrice = product.Price
            };
            // Add the cart item to the database
            _db.CartItems.Add(cartItem);
            _db.SaveChanges();
            return RedirectToAction("Cart");
        }
        [HttpPost]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            var cartItem = _db.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
            if (cartItem != null)
            {
                _db.CartItems.Remove(cartItem);
                _db.SaveChanges();
            }
            return RedirectToAction("Cart");
        }

    }
}
