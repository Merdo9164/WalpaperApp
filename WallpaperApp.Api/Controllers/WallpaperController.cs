using Microsoft.AspNetCore.Mvc;
using WallpaperApp.Application.Interfaces;
using WallpaperApp.Domain.Entities;
using WallpaperApp.Application.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using WallpaperApp.Api.Services;

namespace WallpaperApp.Api.Controllers
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
        [HttpGet]
        public async Task<IActionResult> GetWallpapers()
        {
            var wallpapers = await _repository.GetAllAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var list = wallpapers.Select(w => new
            {
                w.Id,
                w.Title,
                ImageUrl = $"{baseUrl}{w.ImageUrl}",
                ThumbnailUrl = $"{baseUrl}{w.ThumbnailUrl}"
            });

            return Ok(list);
        }

        // GET: api/wallpaper/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetWallpaper(Guid id)
        {
            var wallpaper = await _repository.GetByIdAsync(id);
            if (wallpaper == null)
                return NotFound();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return Ok(new
            {
                wallpaper.Id,
                wallpaper.Title,
                ImageUrl = $"{baseUrl}{wallpaper.ImageUrl}",
                ThumbnailUrl = $"{baseUrl}{wallpaper.ThumbnailUrl}"
            });
        }

        // POST: api/wallpaper/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadWallpaper([FromForm] CreateWallpaperDto dto)
        {
            if (dto == null)
                return BadRequest("Payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title cannot be empty.");

            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest("Image file is required.");

            string imageUrl, thumbnailUrl;

            try
            {
                // Hem ana görseli hem thumbnail’i oluştur
                (imageUrl, thumbnailUrl) = await _fileService.UploadWithThumbnailAsync(dto.Image, dto.Title);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Yükleme sırasında hata: " + ex.Message);
            }

            var wallpaper = new Wallpaper
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                ImageUrl = imageUrl,
                ThumbnailUrl = thumbnailUrl
            };

            await _repository.AddAsync(wallpaper);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            return CreatedAtAction(nameof(GetWallpaper), new { id = wallpaper.Id }, new
            {
                wallpaper.Id,
                wallpaper.Title,
                ImageUrl = $"{baseUrl}{imageUrl}",
                ThumbnailUrl = $"{baseUrl}{thumbnailUrl}"
            });
        }

        // POST: api/wallpaper/download-from-url
        [HttpPost("download-from-url")]
        public async Task<IActionResult> DownloadWallpaperFromUrl([FromBody] CreateWallpaperFromUrlDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.ImageUrl))
                    return BadRequest("Image URL is required.");

                string imageUrl, thumbnailUrl;
                (imageUrl, thumbnailUrl) = await _fileService.DownloadImageAndCreateThumbnailAsync(dto.ImageUrl, dto.Title);

                var wallpaper = new Wallpaper
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title ?? "No Title",
                    ImageUrl = imageUrl,
                    ThumbnailUrl = thumbnailUrl
                };

                await _repository.AddAsync(wallpaper);
                return CreatedAtAction(nameof(GetWallpaper), new { id = wallpaper.Id }, wallpaper);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Beklenmeyen hata: {ex.Message}" });
            }
        }

        // DELETE: api/wallpaper/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteWallpaper(Guid id)
        {
            var wallpaper = await _repository.GetByIdAsync(id);
            if (wallpaper == null)
                return NotFound("Silinecek kayıt bulunamadı.");

            await _fileService.DeleteFileAsync(wallpaper.ImageUrl);
            await _fileService.DeleteFileAsync(wallpaper.ThumbnailUrl);

            await _repository.DeleteAsync(id);
            return Ok(new
            {
                message = "Görsel ve thumbnail başarıyla silindi.",
                deletedId = id
            });
        }
    }
}
