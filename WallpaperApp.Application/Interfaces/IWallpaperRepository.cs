using WallpaperApp.Domain.Entities;

namespace WalpaperApp.Application.Interfaces
{
    public interface IWallpaperRepository
    {
        Task<IEnumerable<Wallpaper>> GetAllAsync();
        Task<Wallpaper> AddAsync(Wallpaper wallpaper); //ekleme metodu
        Task<Wallpaper> GetByIdAsync(int id);
    }
}
