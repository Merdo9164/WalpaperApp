using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WallpaperApp.Domain.Entities;

namespace WallpaperApp.Api.Services
{
    public interface IFileService
    {
        Task<string> UploadAsync(IFormFile file ,string title);
        Task<string> DownloadImageFromUrlAndSaveAsync(string imageUrl , string? title = null);
        Task DeleteFileAsync(string ımageUrl);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _imagesFolder = "images";

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Slugify : "Hello World" -> "hello-world"
        private string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                input = Guid.NewGuid().ToString();

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);
            cleaned = cleaned.ToLowerInvariant();
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-_]", "");// remove invalid chars
            cleaned = Regex.Replace(cleaned, @"[\s_]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = Guid.NewGuid().ToString();

            return cleaned;
        }

        /// <summary>
        /// Formdan yüklenen dosyayı wwwroot/images altına kaydeder.
        /// </summary>
        //Returns /images/{fileName}
        public async Task<string> UploadAsync(IFormFile file , string title)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxFileSize)
                throw new Exception("Dosya boyutu 5MB’dan büyük olamaz.");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new ArgumentException("Desteklenmeyen dosya türü.");


            var slug = Slugify(title);
            var fileName = slug + ext;


            var imagesPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), _imagesFolder);
            Directory.CreateDirectory(imagesPath);


            var fullPath = Path.Combine(imagesPath, fileName);
            int counter = 1;
            while (File.Exists(fullPath))
            {
                // Eğer aynı title ile başka biri yüklemişse "-1", "-2" ekle
                var candidate = $"{slug}-{counter}{ext}";
                fullPath = Path.Combine(imagesPath, candidate);
                counter++;
            }

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


        

            var savedFileName = Path.GetFileName(fullPath);
            return $"/{_imagesFolder}/{savedFileName}";
        }

        /// <summary>
        /// Verilen URL’den resmi indirir, doğrular ve wwwroot/images altına kaydeder.
        /// </summary>
        public async Task<string> DownloadImageFromUrlAndSaveAsync(string imageUrl, string? title = null)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("URL boş olamaz.");

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(imageUrl);

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("Resim indirilemedi. URL geçersiz olabilir veya bağlantı reddedildi.");

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (!contentType.StartsWith("image/"))
                    throw new InvalidOperationException("Belirtilen URL bir resim içermiyor.");

                var ext = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    "image/gif" => ".gif",
                    _ => ".jpg"
                };

                var slug = Slugify(title ?? Path.GetFileNameWithoutExtension(imageUrl) ?? Guid.NewGuid().ToString());
                var imagesPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), _imagesFolder);
                Directory.CreateDirectory(imagesPath);

                var fullPath = Path.Combine(imagesPath, slug + ext);
                int counter = 1;
                while (File.Exists(fullPath))
                {
                    fullPath = Path.Combine(imagesPath, $"{slug}-{counter}{ext}");
                    counter++;
                }
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(fullPath, imageBytes);

                return $"/{_imagesFolder}/{Path.GetFileName(fullPath)}";
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException("Resim indirilemedi. Lütfen internet bağlantınızı veya URL'yi kontrol edin.");
            }
            catch (TaskCanceledException)
            {
                throw new InvalidOperationException("İstek zaman aşımına uğradı. URL çok yavaş veya yanıt vermiyor.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Beklenmeyen bir hata oluştu: {ex.Message}");
            }
        }

        public async Task DeleteFileAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            try
            {
                // URL örneği : "/images/deneme.jpg" veya "https://localhost:7010/images/deneme.jpg"
                //önce relatif path e dönüştürelim
                var fileName = Path.GetFileName(imageUrl);
                var imagesPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images");
                var fullPath = Path.Combine(imagesPath, fileName);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    await Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dosya Silinirken hata oluştu: {ex.Message}");
            }
        }
        private async Task CreateThumbnailAsync(string sourcePath,string destinationPath,int width = 200, int height = 200)
        {
            using var image = await Image.LoadAsync(sourcePath);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Crop

            }));
            await image.SaveAsync(destinationPath, new JpegEncoder { Quality = 85 });
        }

        




    }
}
