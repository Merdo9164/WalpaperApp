using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WallpaperApp.Api.Controllers;
using WallpaperApp.Application.Dtos;
using WallpaperApp.Application.Interfaces;
using WallpaperApp.Domain.Entities;
using Xunit;
using FluentAssertions;
using WallpaperApp.Api.Services;

namespace WallpaperApp.Tests
{
    public class WallpaperControllerTests
    {
        private readonly Mock<IWallpaperRepository> _mockRepo;
        private readonly Mock<IFileService> _mockFileService;
        private readonly WallpaperController _controller;

        public WallpaperControllerTests()
        {
            _mockRepo = new Mock<IWallpaperRepository>();
            _mockFileService = new Mock<IFileService>();
            _controller = new WallpaperController(_mockRepo.Object, _mockFileService.Object);

            // Sahte HTTP RequestContext ekle
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Scheme = "http";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
        }

        [Fact]
        public async Task GetWallpapers_Should_Return_All_Items()
        {
            // Arrange
            var wallpapers = new List<Wallpaper>
            {
                new Wallpaper { Id = Guid.NewGuid(), Title = "Test", ImageUrl = "/images/img1.jpg", ThumbnailUrl = "/thumbnails/thumb1.jpg" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(wallpapers);

            // Act
            var result = await _controller.GetWallpapers(null) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetWallpaper_Should_Return_NotFound_When_NotExists()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Wallpaper?)null);

            // Act
            var result = await _controller.GetWallpaper(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UploadWallpaper_Should_Return_Created_When_Successful()
        {
            // Arrange
            var dto = new CreateWallpaperDto
            {
                Title = "Nice Wallpaper",
                Image = new FormFile(new System.IO.MemoryStream(new byte[10]), 0, 10, "file", "test.jpg")
            };

            _mockFileService.Setup(f => f.UploadWithThumbnailAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync(("/images/test.jpg", "/thumbnails/test.jpg"));

            _mockRepo
                .Setup(r => r.AddAsync(It.IsAny<Wallpaper>()))
                .ReturnsAsync((Wallpaper w) => w);


            if (_controller.ControllerContext == null)
            {
                _controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };
            }
            _controller.ControllerContext.HttpContext.Request.Scheme = "http";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

  

            // Act
            var actionResult = await _controller.UploadWallpaper(dto);

            // Assert
            actionResult.Should().NotBeNull();
            actionResult.Should().BeOfType<CreatedResult>();
            var created = actionResult as CreatedResult;
            created!.Value.Should().NotBeNull();


            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Wallpaper>()), Times.Once);

            var returned = created.Value as dynamic;
        }

        [Fact]
        public async Task DownloadWallpaperFromUrl_Should_Return_Created()
        {
            // Arrange
            var dto = new CreateWallpaperFromUrlDto
            {
                ImageUrl = "https://example.com/image.jpg",
                Title = "Downloaded"
            };

            _mockFileService.Setup(f => f.DownloadImageAndCreateThumbnailAsync(dto.ImageUrl, dto.Title))
                .ReturnsAsync(("/images/img.jpg", "/thumbnails/thumb.jpg"));

            // Act
            var result = await _controller.DownloadWallpaperFromUrl(dto);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public async Task DeleteWallpaper_Should_Return_NotFound_When_Missing()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Wallpaper?)null);

            var result = await _controller.DeleteWallpaper(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteWallpaper_Should_Delete_When_Found()
        {
            var wallpaper = new Wallpaper
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                ImageUrl = "/images/img.jpg",
                ThumbnailUrl = "/thumbnails/thumb.jpg"
            };

            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(wallpaper);

            var result = await _controller.DeleteWallpaper(wallpaper.Id);

            result.Should().BeOfType<OkObjectResult>();
            _mockFileService.Verify(f => f.DeleteFileAsync(wallpaper.ImageUrl), Times.Once);
            _mockFileService.Verify(f => f.DeleteFileAsync(wallpaper.ThumbnailUrl), Times.Once);
            _mockRepo.Verify(r => r.DeleteAsync(wallpaper.Id), Times.Once);
        }
    }
}
