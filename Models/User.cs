using System.ComponentModel.DataAnnotations;

namespace GameStopInventory.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Role { get; set; } // Nullable - set server-side
    }
}