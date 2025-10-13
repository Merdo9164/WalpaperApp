using Microsoft.EntityFrameworkCore;
using WallpaperApp.Domain.Entities;

namespace WalpaperApp.Infrastructure.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        
        public DbSet<Wallpaper> Wallpapers{ get; set; }
    }
}