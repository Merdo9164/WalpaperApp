using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WallpaperApp.Api.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _imagesFolder = "images";

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Formdan yüklenen dosyayı wwwroot/images altına kaydeder.
        /// </summary>
        public async Task<string> UploadAsync(IFormFile file)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxFileSize)
                throw new Exception("Dosya boyutu 5MB’dan büyük olamaz.");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new ArgumentException("Desteklenmeyen dosya türü.");

            var fileName = $"{Guid.NewGuid()}{ext}";
            var imagesPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), _imagesFolder);
            Directory.CreateDirectory(imagesPath);

            var fullPath = Path.Combine(imagesPath, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{_imagesFolder}/{fileName}";
        }

        /// <summary>
        /// Verilen URL’den resmi indirir, doğrular ve wwwroot/images altına kaydeder.
        /// </summary>
        public async Task<string> DownloadImageFromUrlAndSaveAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("URL boş olamaz.");

            using var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                    throw new Exception("Resim indirilemedi. URL geçersiz olabilir.");

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (!contentType.StartsWith("image/"))
                    throw new Exception("Belirtilen URL bir resim içermiyor.");

                var ext = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    "image/gif" => ".gif",
                    _ => ".jpg"
                };

                var fileName = $"{Guid.NewGuid()}{ext}";
                var imagesPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), _imagesFolder);
                Directory.CreateDirectory(imagesPath);

                var fullPath = Path.Combine(imagesPath, fileName);
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(fullPath, imageBytes);

                return $"/{_imagesFolder}/{fileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Resim indirilemedi: {ex.Message}");
            }
        }

        public Task<string> DownloadImageFromUrlAndSaveAsync(string ımageUrl, string v)
        {
            throw new NotImplementedException();
        }
    }
}
