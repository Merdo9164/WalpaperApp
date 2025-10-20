using Microsoft.EntityFrameworkCore;
using WallpaperApp.Domain.Entities;
using WalpaperApp.Application.Interfaces;
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

        // Tüm duvar kağıtlarını getirir
        public async Task<IEnumerable<Wallpaper>> GetAllAsync()
        {
            return await _context.Wallpapers.ToListAsync();
        }

        // Yeni bir duvar kağıdı ekler
        public async Task<Wallpaper> AddAsync(Wallpaper wallpaper)
        {
            _context.Wallpapers.Add(wallpaper);
            await _context.SaveChangesAsync();
            return wallpaper;
        }

        // ID'ye göre duvar kağıdını getirir (Guid kullanımı)
        public async Task<Wallpaper?> GetByIdAsync(Guid id)
        {
            return await _context.Wallpapers.FirstOrDefaultAsync(w => w.Id == id);
        }

        public Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
