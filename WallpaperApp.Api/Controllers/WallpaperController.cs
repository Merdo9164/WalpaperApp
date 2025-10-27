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
        [HttpGet]
        public async Task<IActionResult> GetWallpapers()
        {
            var wallpapers = await _repository.GetAllAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var list = wallpapers.Select(w => new {
                w.Id,
                w.Title,
                ImageUrl = $"{baseUrl}{w.ImageUrl}"
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
            var result = new
            {
                wallpaper.Id,
                wallpaper.Title,
                ImageUrl = $"{baseUrl}{wallpaper.ImageUrl}"
            };    

            return Ok(result);
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
        public async Task<IActionResult> UploadWallpaper([FromForm] CreateWallpaperDto dto)
        {
            if (dto == null)
                return BadRequest("Payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title cannot be empty.");

            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest("Image file is required.");

            string imageUrl;
            try
            {
                imageUrl = await _fileService.UploadAsync(dto.Image, dto.Title);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Yükleme sırasında hata" + ex.Message);
            }

            var wallpaper = new Wallpaper
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                ImageUrl = imageUrl
            };

            await _repository.AddAsync(wallpaper);

            //Tam erişim Urlsi oluştur
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullImageUrl = $"{baseUrl}{imageUrl}";

            // API çıktısında tam URL dönelim
            var result = new
            {
                wallpaper.Id,
                wallpaper.Title,
                ImageUrl = fullImageUrl
            };

            return CreatedAtAction(nameof(GetWallpaper), new { id = wallpaper.Id }, result);
        }

        // POST: api/wallpaper/download-from-url
        [HttpPost("download-from-url")]
        public async Task<IActionResult> DownloadWallpaperFromUrl([FromBody] CreateWallpaperFromUrlDto dto)
        {

            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.ImageUrl))
                    return BadRequest("Image URL is required.");

                string imageUrl;


                imageUrl = await _fileService.DownloadImageFromUrlAndSaveAsync(dto.ImageUrl);

                var wallpaper = new Wallpaper
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title ?? "No Title",
                    ImageUrl = imageUrl
                };

                await _repository.AddAsync(wallpaper);
                return CreatedAtAction(nameof(GetWallpaper), new { id = wallpaper.Id }, wallpaper);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Beklenmeyen hata: {ex.Message}" });
            }

        }
        
        // GET: api/wallpaper/{id}/preview
            [HttpGet("{id:guid}/preview")]
            public async Task<IActionResult> GetWallpaperPreview(Guid id)
            {
                var wallpaper = await _repository.GetByIdAsync(id);
                if (wallpaper == null)
                    return NotFound();

                // Direkt olarak görsel URL'sini dönebiliriz
                return Ok(new { wallpaper.Title, wallpaper.ImageUrl });
            }

        // DELETE: api/wallpaper/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteWallpaper(Guid id)
        {
            var wallpaper = await _repository.GetByIdAsync(id);
            if (wallpaper == null)
                return NotFound(" Silinecek Kayıt Bulunamadı.");


            // Önce dosyayı fiziksel olarak sil
            await _fileService.DeleteFileAsync(wallpaper.ImageUrl);    

            // Ardından veritabanı kaydını sil
            await _repository.DeleteAsync(id);
            return Ok(new
            {
                message = "Görsel Başarıyla Silindi.",
                deletedId = id
            });
        }
    }
}
