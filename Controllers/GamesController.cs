using Azure.Data.Tables;
using Azure.Storage.Blobs;
using GameStopInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStopInventory.Controllers
{
    public class GamesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly TableServiceClient _tableServiceClient;

        public GamesController(AppDbContext context, BlobServiceClient blobServiceClient, TableServiceClient tableServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
            _tableServiceClient = tableServiceClient;
        }

        // GET: /Games
        public async Task<IActionResult> Index()
        {
            var games = await _context.Games.ToListAsync();
            return View(games);
        }

        // GET: /Games/Create
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");
            return View();
        }

        // POST: /Games/Create
        [HttpPost]
        public async Task<IActionResult> Create(Game game, IFormFile? coverImage)
        {
            if (ModelState.IsValid)
            {
                // Upload image to Blob Storage
                if (coverImage != null)
                {
                    var containerClient = _blobServiceClient.GetBlobContainerClient("game-images");
                    await containerClient.CreateIfNotExistsAsync();
                    var blobClient = containerClient.GetBlobClient(coverImage.FileName);
                    using var stream = coverImage.OpenReadStream();
                    await blobClient.UploadAsync(stream, overwrite: true);
                    game.CoverImageUrl = blobClient.Uri.ToString();
                }

                _context.Games.Add(game);
                await _context.SaveChangesAsync();

                // Write audit log to Table Storage
                var tableClient = _tableServiceClient.GetTableClient("AuditLogs");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(new AuditLog
                {
                    Action = "GameAdded",
                    Details = $"Game '{game.Title}' was added by {HttpContext.Session.GetString("Username")}",
                    Timestamp = DateTimeOffset.UtcNow
                });

                return RedirectToAction("Index");
            }
            return View(game);
        }

        // GET: /Games/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound();
            return View(game);
        }

        // POST: /Games/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game != null)
            {
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // GET: /Games/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound();
            return View(game);
        }

        // POST: /Games/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Game game, IFormFile? coverImage)
        {
            if (ModelState.IsValid)
            {
                // Update cover image if a new one was uploaded
                if (coverImage != null)
                {
                    var containerClient = _blobServiceClient.GetBlobContainerClient("game-images");
                    await containerClient.CreateIfNotExistsAsync();
                    var blobClient = containerClient.GetBlobClient(coverImage.FileName);
                    using var stream = coverImage.OpenReadStream();
                    await blobClient.UploadAsync(stream, overwrite: true);
                    game.CoverImageUrl = blobClient.Uri.ToString();
                }

                _context.Games.Update(game);
                await _context.SaveChangesAsync();

                // Audit log
                var tableClient = _tableServiceClient.GetTableClient("AuditLogs");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(new AuditLog
                {
                    Action = "GameUpdated",
                    Details = $"Game '{game.Title}' was updated by {HttpContext.Session.GetString("Username")}",
                    Timestamp = DateTimeOffset.UtcNow
                });

                return RedirectToAction("Index");
            }
            return View(game);
        }
    }
}