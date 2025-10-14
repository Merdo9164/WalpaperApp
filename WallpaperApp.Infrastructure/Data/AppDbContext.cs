using Microsoft.EntityFrameworkCore;
using WallpaperApp.Domain.Entities;

namespace WalpaperApp.Infrastructure.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
    
        public DbSet<Wallpaper> Wallpapers { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallpaper>().HasData(
            new Wallpaper { Id = 1, Title = "Sunset", ImageUrl = "https://example.com/sunset.jpg" },
            new Wallpaper { Id = 2, Title = "Mountains", ImageUrl = "https://example.com/mountains.jpg" }
        );
    }
    }
}