using GameStopInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GameStopInventory.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson == null ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson)!;
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: /Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add(int gameId)
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return NotFound();

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.GameId == gameId);

            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    GameId = game.Id,
                    Title = game.Title,
                    Price = game.Price,
                    Quantity = 1,
                    CoverImageUrl = game.CoverImageUrl
                });
            }

            SaveCart(cart);
            TempData["Message"] = $"{game.Title} added to cart!";
            return RedirectToAction("Index", "Games");
        }

        // POST: /Cart/Remove
        [HttpPost]
        public IActionResult Remove(int gameId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.GameId == gameId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // POST: /Cart/Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();

            if (!cart.Any())
            {
                ViewBag.Error = "Your cart is empty.";
                return View("Index", cart);
            }

            foreach (var item in cart)
            {
                var game = await _context.Games.FindAsync(item.GameId);
                if (game != null)
                {
                    game.StockLevel -= item.Quantity;
                    if (game.StockLevel < 0) game.StockLevel = 0;
                }
            }

            await _context.SaveChangesAsync();
            SaveCart(new List<CartItem>());
            return RedirectToAction("Success");
        }

        // GET: /Cart/Success
        public IActionResult Success() => View();
    }
}