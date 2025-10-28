using Microsoft.EntityFrameworkCore;
using WallpaperApp.Domain.Entities;
using WallpaperApp.Application.Interfaces;
using WallpaperApp.Infrastructure.Data;

namespace WallpaperApp.Infrastructure.Repositories
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

        public async Task<bool> DeleteAsync(Guid id)
        {
            var wallpaper = await _context.Wallpapers.FirstOrDefaultAsync(w => w.Id == id);
            if (wallpaper == null)
                return false;

            _context.Wallpapers.Remove(wallpaper);
            await _context.SaveChangesAsync();
            return true;
        }

        Task IWallpaperRepository.DeleteAsync(Guid id)
        {
            return DeleteAsync(id);
        }
    }
}
