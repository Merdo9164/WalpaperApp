using WallpaperApp.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WalpaperApp.Application.Interfaces
{
    public interface IWallpaperRepository
    {
        Task<IEnumerable<Wallpaper>> GetAllAsync();
        // Sonraki tasklarda ekleyeceğin metotları buraya ekle
        Task<Wallpaper> GetByIdAsync(int id);
        Task AddAsync(Wallpaper wallpaper);
    }
}
