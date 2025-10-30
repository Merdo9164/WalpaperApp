using Xunit;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WallpaperApp.Api.Services;
using System.IO;
using System.Threading.Tasks;

namespace WallpaperApp.Tests.Services
{
    public class FileServiceTests
    {
        private readonly FileService _fileService;
        private readonly Mock<IWebHostEnvironment> _mockEnv;

        public FileServiceTests()
        {
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.WebRootPath).Returns(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            _fileService = new FileService(_mockEnv.Object);
        }

        [Fact]
        public async Task UploadAsync_ShouldReturnFilePath_WhenFileIsValid()
        {
            //Arrange
            var fileMock = new Mock<IFormFile>();
            var content = "Fake image content";
            var fileName = "test.jpeg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns((Stream stream, System.Threading.CancellationToken token) =>
            {
                ms.CopyTo(stream);
                return Task.CompletedTask;
            });

            //Act
            var result = await _fileService.UploadAsync(fileMock.Object, "TestTitle");


            //Assert
            Assert.Contains("/images/", result);

            //UploadAsync valid bir dosya için doğru yolu dönüyor mu .
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldNotThrow_WhenFileDoesNotExist()
        {
            //Arrange
            string fakePath = "/images/nonexistent.jpg";

            //act & Assert
            var exception = await Record.ExceptionAsync(() => _fileService.DeleteFileAsync(fakePath));
            Assert.Null(exception);

            //DeleteFileAsync var olmayan bir dosyada hata firlatıyor mu .
        }
    }
}