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
        public async Task<IActionResult> GetWallpapers([FromQuery] string? title)
        {
            var wallpapers = await _repository.GetAllAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // Filtreleme 
            if (!string.IsNullOrWhiteSpace(title))
            {
                var slugTitle = _fileService.Slugify(title);
                wallpapers = wallpapers
                    .Where(w =>
                        w.Title.Equals(title, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(w.ImageUrl) &&
                        w.ImageUrl.Contains($"/images/{slugTitle}", StringComparison.OrdinalIgnoreCase))
                    )
                    .ToList();
            }

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

            //Tekli veya çoklu dosya var mı kontrol et
            if ((dto.Images == null || dto.Images.Count == 0) && (dto.Image == null))
                return BadRequest(" At least one Image file is required.");

            var uploadedResults = new List<(string ImageUrl, string ThumbnailUrl)>();    

            try
            {
                // Çoklu dosya varsa -klasörlü sistemle kaydet
                if (dto.Images != null && dto.Images.Count > 0)
                {
                    uploadedResults = await _fileService.UploadMultipleWithThumbnailAsync(dto.Images, dto.Title);
                }
                else if (dto.Image != null)
                {
                    //Tekli dosya varsa
                    var singleResult = await _fileService.UploadWithThumbnailAsync(dto.Image, dto.Title);
                    uploadedResults.Add(singleResult);
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Yükleme sırasında hata: " + ex.Message);
            }

            //Veri tabanına kayıt

            var wallpapers = new List<Wallpaper>();
            foreach (var (imageUrl, thumbnailUrl) in uploadedResults)
            {
                var wallpaper = new Wallpaper
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    ImageUrl = imageUrl,
                    ThumbnailUrl = thumbnailUrl
                };
                wallpapers.Add(wallpaper);
                await _repository.AddAsync(wallpaper);

            }

            //Cevap Döndür
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var response = wallpapers.Select(w => new
            {
                w.Id,
                w.Title,
                ImageUrl = $"{baseUrl}{w.ImageUrl}",
                ThumbnailUrl = $"{baseUrl}{w.ThumbnailUrl}"

            });

            return Created(string.Empty, response);
            
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

        // PUT: api/wallpaper/update/{id}
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateWallpaper(Guid id, [FromBody] UpdateWallpaperDto dto)
        {
            var wallpaper = await _repository.GetByIdAsync(id);
            if (wallpaper == null)
                return NotFound("Görsel bulunamadı.");

            var oldTitle = wallpaper.Title;
            var newTitle = dto.Title;
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            //  Fiziksel dosya adını değiştir (dosya aynı yerde kalır)
            await _fileService.UpdateNewTitleAsync(oldTitle, newTitle);

            //  Yeni slug oluştur
            var newSlug = _fileService.Slugify(newTitle);

            //  Eski URL’den dizin adını çıkar
            var imageExt = Path.GetExtension(wallpaper.ImageUrl);
            var imageDir = Path.GetDirectoryName(wallpaper.ImageUrl.Replace('/', Path.DirectorySeparatorChar))!
                .Replace(Path.DirectorySeparatorChar, '/');

            var thumbExt = Path.GetExtension(wallpaper.ThumbnailUrl);

            //  Yeni URL’leri oluştur (aynı klasörde kalır)
            wallpaper.ImageUrl = $"{imageDir}/{newSlug}{imageExt}";
            wallpaper.ThumbnailUrl = $"{imageDir.Replace("/images", "/thumbnails")}/{newSlug}{thumbExt}";
            wallpaper.Title = newTitle;

            //  Veritabanında güncelle
            await _repository.UpdateAsync(wallpaper);

            return Ok(new
            {
                wallpaper.Id,
                wallpaper.Title,
                ImageUrl = $"{baseUrl}{wallpaper.ImageUrl}",
                ThumbnailUrl = $"{baseUrl}{wallpaper.ThumbnailUrl}"
            });
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
