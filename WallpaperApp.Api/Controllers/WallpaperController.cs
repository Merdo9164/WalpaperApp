using Microsoft.AspNetCore.Mvc;
using WallpaperApp.Application.Dtos;       // DTO için
using WalpaperApp.Application.Interfaces; // Repository interface

namespace WalpaperApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WallpaperController : ControllerBase
    {
        private readonly IWallpaperRepository _repository;

        public WallpaperController(IWallpaperRepository repository)
        {
            _repository = repository;
        }

        // GET: api/wallpaper
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WallpaperDto>>> GetWallpapers()
        {
            // Repository'den tüm wallpaper'ları çek
            var wallpapers = await _repository.GetAllAsync();

            // Entity'yi DTO'ya dönüştür
            var wallpaperDtos = wallpapers.Select(w => new WallpaperDto
            {
                Id = w.Id,
                Title = w.Title,
                ImageUrl = w.ImageUrl
            });

            return Ok(wallpaperDtos);
        }
    }
}
