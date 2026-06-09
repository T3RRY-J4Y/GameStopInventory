using GameStopInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStopInventory.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(
            [Bind("Username,Email,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                bool userExists = await _context.Users
                    .AnyAsync(u => u.Username == user.Username);

                if (userExists)
                {
                    ModelState.AddModelError("Username", "That username is already taken.");
                    return View(user);
                }

                bool emailExists = await _context.Users
                    .AnyAsync(u => u.Email == user.Email);

                if (emailExists)
                {
                    ModelState.AddModelError("Email", "An account with that email already exists.");
                    return View(user);
                }

                user.Role = "Customer";
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            return View(user);
        }

        // GET: /Account/Login
        public IActionResult Login() => View();

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter both username and password.";
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                return RedirectToAction("Index", "Games");
            }

            ViewBag.Error = "Incorrect username or password. Please try again.";
            return View();
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}