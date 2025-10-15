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

        [HttpPost]
        public async Task<ActionResult<WallpaperDto>> CreateWallpaper([FromBody] CreateWallpaperDto createDto)
        {
            if (string.IsNullOrWhiteSpace(createDto.Title) || string.IsNullOrWhiteSpace(createDto.ImageUrl))
            {
                return BadRequest("Title and ImageUrl are required"); // eksik alan varsa dönülür
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

            return CreatedAtAction(nameof(GetWallpapers), new { id = created.Id }, result); // eklenen veri başarıyla döner

            // Dto ve Entity dönüşümü yapılır
            //Kullanıcıya doğru HTTP durum kodlarını döndürür

        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<WallpaperDto>> GetWallpaper(int id)
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
         




    }



 }

