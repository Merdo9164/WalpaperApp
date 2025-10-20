using Microsoft.AspNetCore.Mvc;
using WallpaperApp.Application.Dtos;
using WallpaperApp.Domain.Entities;
using WalpaperApp.Application.Interfaces;

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

        // 🔹 Tüm duvar kağıtlarını getir
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WallpaperDto>>> GetWallpapers()
        {
            var wallpapers = await _repository.GetAllAsync();

            var wallpaperDtos = wallpapers.Select(w => new WallpaperDto
            {
                Id = w.Id,
                Title = w.Title,
                ImageUrl = w.ImageUrl
            });

            return Ok(wallpaperDtos);
        }

        // 🔹 Yeni duvar kağıdı ekle
        [HttpPost]
        public async Task<ActionResult<WallpaperDto>> CreateWallpaper([FromBody] CreateWallpaperDto createDto)
        {
            if (string.IsNullOrWhiteSpace(createDto.Title) || string.IsNullOrWhiteSpace(createDto.ImageUrl))
            {
                return BadRequest("Title and ImageUrl are required");
            }

            var wallpaper = new Wallpaper
            {
                Title = createDto.Title,
                ImageUrl = createDto.ImageUrl
            };

            var created = await _repository.AddAsync(wallpaper);

            var result = new WallpaperDto
            {
                Id = created.Id,
                Title = created.Title,
                ImageUrl = created.ImageUrl
            };

            return CreatedAtAction(nameof(GetWallpaper), new { id = created.Id }, result);
        }

        // 🔹 ID (Guid) ile duvar kağıdı getir
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<WallpaperDto>> GetWallpaper(Guid id)
        {
            try
            {
                var wallpaper = await _repository.GetByIdAsync(id);

                if (wallpaper == null)
                    return NotFound();

                var dto = new WallpaperDto
                {
                    Id = wallpaper.Id,
                    Title = wallpaper.Title,
                    ImageUrl = wallpaper.ImageUrl
                };

                return Ok(dto);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
