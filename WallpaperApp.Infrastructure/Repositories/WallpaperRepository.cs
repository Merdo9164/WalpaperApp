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

        public async Task<IEnumerable<Wallpaper>> GetAllAsync()
        {
            return await _context.Wallpapers.ToListAsync();
        }

        public async Task<Wallpaper> AddAsync(Wallpaper wallpaper)
        {
            _context.Wallpapers.Add(wallpaper);
            await _context.SaveChangesAsync();
            return wallpaper;
        }

        public async Task<Wallpaper?> GetByIdAsync(int id)
        {
            return await _context.Wallpapers.FirstOrDefaultAsync(w => w.Id == id);
        }
        
        
    }
}
