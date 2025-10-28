using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WallpaperApp.Application.Interfaces;


namespace WallpaperApp.Api.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _imagesFolder = "images";
        private readonly string _thumbnailsFolder = "thumbnails";

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Title'dan güvenli dosya ismi üretir
        private string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                input = Guid.NewGuid().ToString();

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC)
                .ToLowerInvariant();

            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-_]", "");
            cleaned = Regex.Replace(cleaned, @"[\s_]+", "-").Trim('-');

            return string.IsNullOrWhiteSpace(cleaned) ? Guid.NewGuid().ToString() : cleaned;
        }

        //  Asıl görsel + thumbnail oluşturur, yollarını döner
        public async Task<(string imageUrl, string thumbnailUrl)> UploadWithThumbnailAsync(IFormFile file, string title)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
                throw new Exception("Dosya boyutu 5MB’dan büyük olamaz.");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new ArgumentException("Desteklenmeyen dosya türü.");

            var slug = Slugify(title);
            var fileName = $"{slug}{ext}";

            var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            //  images klasörüne kaydet
            var imagesPath = Path.Combine(wwwroot, _imagesFolder);
            Directory.CreateDirectory(imagesPath);
            var fullImagePath = Path.Combine(imagesPath, fileName);

            int counter = 1;
            while (File.Exists(fullImagePath))
            {
                fileName = $"{slug}-{counter}{ext}";
                fullImagePath = Path.Combine(imagesPath, fileName);
                counter++;
            }

            using (var stream = new FileStream(fullImagePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            //  thumbnails klasörüne kaydet
            var thumbnailsPath = Path.Combine(wwwroot, _thumbnailsFolder);
            Directory.CreateDirectory(thumbnailsPath);
            var thumbnailFullPath = Path.Combine(thumbnailsPath, fileName);

            await CreateThumbnailAsync(fullImagePath, thumbnailFullPath);

            return ($"/{_imagesFolder}/{fileName}", $"/{_thumbnailsFolder}/{fileName}");
        }

        //  URL'den indirip hem ana resmi hem thumbnail'i oluşturur
        public async Task<(string imageUrl, string thumbnailUrl)> DownloadImageAndCreateThumbnailAsync(string imageUrl, string? title = null)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("URL boş olamaz.");

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(imageUrl);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Resim indirilemedi.");

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.StartsWith("image/"))
                throw new InvalidOperationException("URL bir resim içermiyor.");

            var ext = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };

            var slug = Slugify(title ?? Path.GetFileNameWithoutExtension(imageUrl) ?? Guid.NewGuid().ToString());
            var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var imagesPath = Path.Combine(wwwroot, _imagesFolder);
            Directory.CreateDirectory(imagesPath);

            var fullImagePath = Path.Combine(imagesPath, slug + ext);
            int counter = 1;
            while (File.Exists(fullImagePath))
            {
                fullImagePath = Path.Combine(imagesPath, $"{slug}-{counter}{ext}");
                counter++;
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(fullImagePath, imageBytes);

            //  Thumbnail oluştur
            var thumbnailsPath = Path.Combine(wwwroot, _thumbnailsFolder);
            Directory.CreateDirectory(thumbnailsPath);
            var thumbnailFullPath = Path.Combine(thumbnailsPath, Path.GetFileName(fullImagePath));

            await CreateThumbnailAsync(fullImagePath, thumbnailFullPath);

            return ($"/{_imagesFolder}/{Path.GetFileName(fullImagePath)}",
                    $"/{_thumbnailsFolder}/{Path.GetFileName(fullImagePath)}");
        }

        //  Dosya siler
        public async Task DeleteFileAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            try
            {
                var fileName = Path.GetFileName(imageUrl);
                var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var fullPath = Path.Combine(wwwroot, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dosya silinirken hata: {ex.Message}");
            }
        }

        //  Thumbnail oluşturur (Crop + orantılı)
        private async Task CreateThumbnailAsync(string sourcePath, string destinationPath, int width = 200, int height = 200)
        {
            using var image = await Image.LoadAsync(sourcePath);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Crop //  Görseli orantılı kırparak 200x200 yapar
            }));
            await image.SaveAsync(destinationPath, new JpegEncoder { Quality = 85 });
        }

        public Task<string> UploadAsync(IFormFile file, string title)
        {
            throw new NotImplementedException();
        }

        public Task<string> DownloadImageFromUrlAndSaveAsync(string imageUrl)
        {
            throw new NotImplementedException();
        }
    }
}
