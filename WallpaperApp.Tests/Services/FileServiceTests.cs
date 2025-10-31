using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using WallpaperApp.Api.Services;
using Xunit;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace WallpaperApp.Tests
{
    public class FileServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly FileService _fileService;
        private readonly string _testRoot;

        public FileServiceTests()
        {
            _mockEnv = new Mock<IWebHostEnvironment>();
            _testRoot = Path.Combine(Path.GetTempPath(), "WallpaperTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testRoot);

            _mockEnv.Setup(e => e.WebRootPath).Returns(_testRoot);
            _fileService = new FileService(_mockEnv.Object);
        }

        //  Artık gerçekten geçerli bir 1x1 piksel JPEG üretiyoruz
        private IFormFile CreateFakeImage(string fileName = "test.jpg", int width = 1, int height = 1)
        {
            var image = new Image<Rgba32>(width, height);
            image[0, 0] = new Rgba32(255, 0, 0); // 1 kırmızı piksel

            var stream = new MemoryStream();
            image.Save(stream, new JpegEncoder());
            stream.Position = 0;

            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };
        }

        [Fact]
        public async Task UploadWithThumbnailAsync_Should_Save_Image_And_Thumbnail()
        {
            // Arrange
            var fakeImage = CreateFakeImage("wallpaper.jpg");

            // Act
            var (imageUrl, thumbnailUrl) = await _fileService.UploadWithThumbnailAsync(fakeImage, "My Test Image");

            // Assert
            imageUrl.Should().Contain("/images/");
            thumbnailUrl.Should().Contain("/thumbnails/");

            var imagePath = Path.Combine(_testRoot, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var thumbPath = Path.Combine(_testRoot, thumbnailUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            File.Exists(imagePath).Should().BeTrue($"image not found at {imagePath}");
            File.Exists(thumbPath).Should().BeTrue($"thumbnail not found at {thumbPath}");
        }

        [Fact]
        public async Task UploadWithThumbnailAsync_Should_Throw_When_File_Too_Large()
        {
            // Arrange – büyük dosya oluştur
            var bigStream = new MemoryStream(new byte[6 * 1024 * 1024]); // 6MB
            var bigFile = new FormFile(bigStream, 0, bigStream.Length, "file", "big.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            // Act
            Func<Task> act = async () => await _fileService.UploadWithThumbnailAsync(bigFile, "Large File");

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*5MB’dan büyük olamaz*");
        }

        [Fact]
        public async Task UploadWithThumbnailAsync_Should_Throw_When_Invalid_Extension()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            var fakeFile = new FormFile(stream, 0, stream.Length, "file", "bad.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            // Act
            Func<Task> act = async () => await _fileService.UploadWithThumbnailAsync(fakeFile, "Invalid File");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Desteklenmeyen dosya türü*");
        }

        [Fact]
        public async Task DeleteFileAsync_Should_Remove_File_If_Exists()
        {
            // Arrange
            var filePath = Path.Combine(_testRoot, "images", "toDelete.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, "testdata");

            // Act
            await _fileService.DeleteFileAsync("/images/toDelete.jpg");

            // Assert
            File.Exists(filePath).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteFileAsync_Should_Not_Throw_When_File_Not_Exists()
        {
            // Act
            Func<Task> act = async () => await _fileService.DeleteFileAsync("/images/nonexistent.jpg");

            // Assert
            await act.Should().NotThrowAsync();
        }

        //  Cleanup
        ~FileServiceTests()
        {
            try
            {
                if (Directory.Exists(_testRoot))
                    Directory.Delete(_testRoot, true);
            }
            catch { /* ignore */ }
        }
    }
}
