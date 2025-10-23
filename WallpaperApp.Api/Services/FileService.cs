using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WallpaperApp.Api.Services
{
    public interface IFileService
    {
        Task<string> UploadAsync(IFormFile file);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _imagesFolder = "images";

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadAsync(IFormFile file)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB

            if (file.Length > maxFileSize)
            throw new Exception("Dosya boyutu 5MB’dan büyük olamaz.");

            // Temel güvenlik: sadece belirli uzantılara izin ver (jpg/png/webp vb.)
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new ArgumentException("Desteklenmeyen dosya türü.");

            // GUID ile isimlendir
            var fileName = $"{Guid.NewGuid()}{ext}";

            // wwwroot/images yolunu hazırla
            var imagesPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), _imagesFolder);
            Directory.CreateDirectory(imagesPath);

            var fullPath = Path.Combine(imagesPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Erişim URL'i (relatif)., sadece path yeterli: /images/{fileName}
            return $"/{_imagesFolder}/{fileName}";
        }
    }
}
