using Microsoft.EntityFrameworkCore;
using CartService.Models;

namespace CartService.Data
{
    public class CartDbContext : DbContext
    {
        public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
        {
        }

        public DbSet<CartItem> CartItems { get; set; }
    }
}
