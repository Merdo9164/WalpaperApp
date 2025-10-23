using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WalpaperApp.Application.Interfaces;
using WallpaperApp.Domain.Entities;
using System;
using System.Threading.Tasks;
using WallpaperApp.Application.Dtos;
using WallpaperApp.Api.Services;

namespace WalpaperApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WallpaperController : ControllerBase
    {
        private readonly IWallpaperRepository _repository;
        private readonly IFileService _fileService;

        public WallpaperController(IWallpaperRepository repository, IFileService fileService)
        {
            _repository = repository;
            _fileService = fileService;
        }

        // GET: api/wallpaper
        /// <summary>
        /// Sistemdeki tüm duvar kağıtlarını döner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWallpapers()
        {
            var wallpapers = await _repository.GetAllAsync();
            return Ok(wallpapers);
        }

        // GET: api/wallpaper/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetWallpaper(Guid id)
        {
            var wallpaper = await _repository.GetByIdAsync(id);
            if (wallpaper == null)
                return NotFound();

            return Ok(wallpaper);
        }

        // POST: api/wallpaper
        [HttpPost]
        public async Task<IActionResult> PostWallpaper([FromBody] Wallpaper wallpaper)
        {
            if (wallpaper == null)
                return BadRequest("Wallpaper cannot be null.");

            if (string.IsNullOrWhiteSpace(wallpaper.Title))
                return BadRequest("Title cannot be empty.");

            if (string.IsNullOrWhiteSpace(wallpaper.ImageUrl))
                return BadRequest("ImageUrl cannot be empty.");

            wallpaper.Id = Guid.NewGuid();
            await _repository.AddAsync(wallpaper);

            return CreatedAtAction(nameof(GetWallpaper), new { id = wallpaper.Id }, wallpaper);
        }

        // POST: api/wallpaper/upload
        [HttpPost("upload")]
        public async Task<IActionResult> PostWallpaper([FromForm] CreateWallpaperDto dto)
        {
            if (dto == null)
                return BadRequest("Payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title cannot be empty.");

            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest("Image file is required.");

            // Dosyayı yükle
            string imageUrl;
            try
            {
                imageUrl = await _fileService.UploadAsync(dto.Image);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            var wallpaper = new Wallpaper
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                ImageUrl = imageUrl
            };

            await _repository.AddAsync(wallpaper);

            return CreatedAtAction(nameof(GetWallpaper), new { id = wallpaper.Id }, wallpaper);
        }

        // POST: api/wallpaper/download-from-url
        /// <summary>
        /// URL üzerinden bir resmi indirip sunucuya kaydeder.
        /// </summary>
        [HttpPost("download-from-url")]
        public async Task<IActionResult> DownloadFromUrl([FromBody] ImageUrlRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageUrl))
                return BadRequest("Lütfen geçerli bir URL giriniz.");

            try
            {
                // Serviste URL’den indirilen resmi kaydet
                var savedPath = await _fileService.DownloadImageFromUrlAndSaveAsync(request.ImageUrl, "URL'den gelen resim");

                // Veritabanına da kaydedelim
                var wallpaper = new Wallpaper
                {
                    Id = Guid.NewGuid(),
                    Title = "URL'den Gelen Resim",
                    ImageUrl = savedPath
                };

                await _repository.AddAsync(wallpaper);

                return Ok(new
                {
                    message = "Resim başarıyla indirildi ve kaydedildi.",
                    imageUrl = savedPath
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }




        // DELETE: api/wallpaper/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteWallpaper(Guid id)
        {
            var wallpaper = await _repository.GetByIdAsync(id);
            if (wallpaper == null)
                return NotFound();

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
    
    /// <summary>
    /// URL’den görsel indirme isteği için DTO.
    /// </summary>
    public class ImageUrlRequest
    {
        public string ImageUrl { get; set; }
    }
}
