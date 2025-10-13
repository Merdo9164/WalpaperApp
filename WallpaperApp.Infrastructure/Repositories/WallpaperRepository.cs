using Microsoft.EntityFrameworkCore;
using WallpaperApp.Domain.Entities;
using WalpaperApp.Infrastructure.Data;

namespace WalpaperApp.Infrastructure.Repositories
{
    public class WallpaperRepository : IWallpaperRepository
    {
        private readonly AppDbContext _context;

        public WallpaperRepository(AppDbContext context)
        {
            _context = context;
        }

        // BURAYA EKLE: Veritabanı sorgusu
        public async Task<IEnumerable<Wallpaper>> GetAllAsync()
        {
            return await _context.Wallpapers.ToListAsync();
        }
    }

    public interface IWallpaperRepository
    {
    }
}
