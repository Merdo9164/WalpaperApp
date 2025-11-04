using WallpaperApp.Domain.Entities;

namespace WallpaperApp.Application.Interfaces
{
    public interface IWallpaperRepository
    {
        Task<IEnumerable<Wallpaper>> GetAllAsync();
        Task<Wallpaper> AddAsync(Wallpaper wallpaper); //ekleme metodu
        Task<Wallpaper?> GetByIdAsync(Guid id);
        Task UpdateAsync(Wallpaper wallpaper);

        Task DeleteAsync(Guid id);

    }
}
