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
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;


namespace WallpaperApp.Api.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        

        private readonly string _imagesFolder = "images";
        private readonly string _thumbnailsFolder = "thumbnails";

        public FileService(IWebHostEnvironment env )
        {
            _env = env;
        }

        // Title'dan güvenli dosya ismi üretir
        public string Slugify(string input)
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
        // 5 mb dan büyük yüklenen görselleri uygun hale getirip yükler
        public async Task<(string imageUrl, string thumbnailUrl)> UploadWithThumbnailAsync(IFormFile file, string title)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Dosya boş veya yüklenemedi.");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new ArgumentException("Desteklenmeyen dosya türü.");


            //url leri title olarak döndür
            var slug = Slugify(title);
            var fileName = $"{slug}{ext}";

            var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var imagesPath = Path.Combine(wwwroot, _imagesFolder);
            Directory.CreateDirectory(imagesPath);
            var fullImagePath = Path.Combine(imagesPath, fileName);

            var thumbnailsPath = Path.Combine(wwwroot, _thumbnailsFolder);
            Directory.CreateDirectory(thumbnailsPath);
            var thumbnailFullPath = Path.Combine(thumbnailsPath, fileName);

            //  Görseli oku (ImageSharp destekler)
            using var image = await Image.LoadAsync(file.OpenReadStream());

            //  Eğer çok büyükse yeniden boyutlandır (örneğin >1920x1080)
            if (image.Width > 1920 || image.Height > 1080)
            {
                var resizeOptions = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1920, 1080)
                };
                image.Mutate(x => x.Resize(resizeOptions));
            }

            //  Optimize edilip .jpg olarak kaydet
            await image.SaveAsync(fullImagePath, new JpegEncoder { Quality = 85 });

            // Thumbnail oluştur (200x200)
            using var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(200, 200)
            }));

            await thumbnail.SaveAsync(thumbnailFullPath, new JpegEncoder { Quality = 75 });

            // URL'leri döndür
            return ($"/{_imagesFolder}/{fileName}", $"/{_thumbnailsFolder}/{fileName}");
        }

        // toplu görsel yükleme
        public async Task <List<(string ImageUrl, string ThumbnailUrl)>> UploadMultipleWithThumbnailAsync(List<IFormFile> files , string title)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentException("At least one image file is required.");


            //Root Path belirleniyor
            var slugTitle = Slugify(title);
            var uploadPath = Path.Combine(_env.WebRootPath, "images", slugTitle);
            var thumbPath = Path.Combine(_env.WebRootPath, "thumbnails", slugTitle);

            Directory.CreateDirectory(uploadPath);
            Directory.CreateDirectory(thumbPath);

            //GEri dönecek liste
            var uploadedFiles = new List<(string ImageUrl, string ThumbnailUrl)>();

            foreach (var file in files)
            {
                if (file.Length <= 0)
                    continue;

                // Dosya adı güvenli hale getir
                var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                var ext = Path.GetExtension(file.FileName);
                var safeName = Slugify(originalName) + ext;

                var imagePath = Path.Combine(uploadPath, safeName);
                var thumbFilePath = Path.Combine(thumbPath, safeName);

                // Görseli kaydet
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Thumbnail oluştur 
                using (var image = Image.Load(file.OpenReadStream()))
                {
                    int thumbWidth = 300;
                    int thumbHeight = (int)(image.Height * (thumbWidth / (float)image.Width));
                    image.Mutate(x => x.Resize(thumbWidth, thumbHeight));
                    image.Save(thumbFilePath);
                }

                // URL'leri hazırla
                var imageUrl = $"/images/{slugTitle}/{safeName}";
                var thumbUrl = $"/thumbnails/{slugTitle}/{safeName}";

                uploadedFiles.Add((imageUrl, thumbUrl));
            }

            return uploadedFiles;  
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
        //update 
        public async Task UpdateNewTitleAsync(string oldTitle, string newTitle)
        {
            if (string.IsNullOrWhiteSpace(oldTitle) || string.IsNullOrWhiteSpace(newTitle))
                return;

            var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var oldSlug = Slugify(oldTitle);
            var newSlug = Slugify(newTitle);

            var imageRoot = Path.Combine(wwwroot, "images");
            var thumbRoot = Path.Combine(wwwroot, "thumbnails");

            // Images klasöründeki tüm alt klasörleri tarar
            var imageFiles = Directory.GetFiles(imageRoot, "*.*", SearchOption.AllDirectories)
                .Where(f => Path.GetFileNameWithoutExtension(f).Contains(oldSlug, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in imageFiles)
            {
                var ext = Path.GetExtension(file);
                var dir = Path.GetDirectoryName(file)!;
                var newFilePath = Path.Combine(dir, newSlug + ext);

                // Eğer aynı isimde dosya yoksa yeniden adlandır
                if (!File.Exists(newFilePath))
                {
                    File.Move(file, newFilePath);
                    Console.WriteLine($"[UpdateNewTitleAsync] Image renamed: {file} -> {newFilePath}");
                }
            }

            // Thumbnails klasörünü de aynı şekilde kontrol eder
            var thumbFiles = Directory.GetFiles(thumbRoot, "*.*", SearchOption.AllDirectories)
                .Where(f => Path.GetFileNameWithoutExtension(f).Contains(oldSlug, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in thumbFiles)
            {
                var ext = Path.GetExtension(file);
                var dir = Path.GetDirectoryName(file)!;
                var newFilePath = Path.Combine(dir, newSlug + ext);

                if (!File.Exists(newFilePath))
                {
                    File.Move(file, newFilePath);
                    Console.WriteLine($"[UpdateNewTitleAsync] Thumbnail renamed: {file} -> {newFilePath}");
                }
            }

            await Task.CompletedTask;
        }



        //  Dosya siler
        public async Task DeleteFileAsync(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            try
            {
                var uri = new Uri(imageUrl, UriKind.RelativeOrAbsolute);
                var relativePath = uri.IsAbsoluteUri ? uri.AbsolutePath : imageUrl;
                relativePath = relativePath.TrimStart('/');

                
                var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var fullPath = Path.Combine(wwwroot, relativePath.Replace('/', Path.DirectorySeparatorChar));

                var thumbPath = fullPath.Replace(Path.Combine("images", ""), Path.Combine("thumbnails", ""));


                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                if (File.Exists(thumbPath))
                    File.Delete(thumbPath);

                //klasör temizliği (boşsa sil)
                var directory = Path.GetDirectoryName(fullPath);
                if(!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    if(!Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory);
                        Console.WriteLine($"Boş Klasör Silindi: {directory}");
                    }
                }

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
                Mode = ResizeMode.Max, // oranı koruyarak küçültür.
                Sampler = KnownResamplers.Lanczos3,
                Compand = true
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

        public Task<(string ImageUrl, string ThumbnailUrl)> UploadMultipleWithThumbnailAsync(IFormFile file, string title)
        {
            throw new NotImplementedException();
        }
    }
}
