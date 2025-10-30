using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WallpaperApp.Domain.Entities;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using WallpaperApp.Api.Services;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace WallpaperApp.Tests.Controllers
{

    public interface IThumbnailService
    {
        Task<string> GenerateThumbnailAsync(string imagePath);
    }
    public class WallpaperControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public WallpaperControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        // GET endpoint testi
        [Fact]
        public async Task GetWallpapers_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/wallpaper");
            response.EnsureSuccessStatusCode();

            var wallpapers = await response.Content.ReadFromJsonAsync<List<Wallpaper>?>();
            Assert.NotNull(wallpapers);
        }

        // POST endpoint testi (entity kullanımı ve GUID uyumu)
        [Fact]
        public async Task PostWallpaper_WithValidData_ReturnsCreated()
        {
            var newWallpaper = new Wallpaper
            {
                Title = "Test Wallpaper",
                ImageUrl = "http://example.com/test.jpg"
            };

            var response = await _client.PostAsJsonAsync("/api/wallpaper", newWallpaper);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<Wallpaper?>();
            Assert.NotNull(created);
            Assert.Equal("Test Wallpaper", created!.Title);
            Assert.Equal("http://example.com/test.jpg", created.ImageUrl);
            Assert.NotEqual(Guid.Empty, created.Id); // GUID kontrolü
        }

        // GET by GUID testi
        [Fact]
        public async Task GetWallpaper_ByGuid_ReturnsWallpaper()
        {
            var newWallpaper = new Wallpaper
            {
                Title = "Another Image",
                ImageUrl = "http://example.com/another.jpg"
            };

            var postResponse = await _client.PostAsJsonAsync("/api/wallpaper", newWallpaper);
            var created = await postResponse.Content.ReadFromJsonAsync<Wallpaper?>();
            Assert.NotNull(created);

            var getResponse = await _client.GetAsync($"/api/wallpaper/{created!.Id}");
            getResponse.EnsureSuccessStatusCode();

            var fetched = await getResponse.Content.ReadFromJsonAsync<Wallpaper?>();
            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched!.Id);
            Assert.Equal("Another Image", fetched.Title);
        }

        //Delete Endpoint testi
        [Fact]
        public async Task DeleteWallpaper_ExistingWallpaper_ReturnsNoContent()
        {
            var newWallpaper = new Wallpaper
            {
                Title = "Delete Test",
                ImageUrl = "http://example.com/delete.jpg"
            };

            var postResponse = await _client.PatchAsJsonAsync("/api/wallpaper", newWallpaper);
            var created = await postResponse.Content.ReadFromJsonAsync<Wallpaper?>();
            Assert.NotNull(created);

            var deleteResponse = await _client.DeleteAsync($"/api/wallpaper/{created!.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        // Boş title ile POST
        [Fact]
        public async Task PostWallpaper_WithEmptyTitle_ReturnsBadRequest()
        {
            var invalidWallpaper = new Wallpaper
            {
                Title = "",
                ImageUrl = "http://example.com/valid.jpg"
            };

            var response = await _client.PostAsJsonAsync("/api/wallpaper", invalidWallpaper);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // Boş ImageUrl ile POST
        [Fact]
        public async Task PostWallpaper_WithEmptyImageUrl_ReturnsBadRequest()
        {
            var invalidWallpaper = new Wallpaper
            {
                Title = "Valid Title",
                ImageUrl = ""
            };

            var response = await _client.PostAsJsonAsync("/api/wallpaper", invalidWallpaper);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // Null object ile POST
        [Fact]
        public async Task PostWallpaper_WithNull_ReturnsBadRequest()
        {
            Wallpaper? invalidWallpaper = null;

            var response = await _client.PostAsJsonAsync("/api/wallpaper", invalidWallpaper);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // Yanlış endpoint ile GET
        [Fact]
        public async Task GetWallpapers_WrongUrl_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/wallpapers-wrong");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // GET list test (boş veya dolu)
        [Fact]
        public async Task GetWallpapers_ReturnsListSuccessfully()
        {
            var response = await _client.GetAsync("/api/wallpaper");
            response.EnsureSuccessStatusCode();

            var wallpapers = await response.Content.ReadFromJsonAsync<List<Wallpaper>?>();
            Assert.NotNull(wallpapers);
            Assert.True(wallpapers!.Count >= 0);
        }

        //FileService mock Testi
        [Fact]
        public async Task FileService_UploadAsync_ReturnsFilePathSuccessfully()
        {
            var mockFileService = new Mock<IFileService>();
            var mockFile = new Mock<IFormFile>();

            mockFileService
                .Setup(fs => fs.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("/uploads/test.jpg");

            var result = await mockFileService.Object.UploadAsync(mockFile.Object, "test");
            Assert.Equal("/uploads/test.jpg", result);
        }


        //ThumbnailService Mock Testi
        [Fact]
        public async Task ThumbnailService_GenerateThumbnailAsync_ReturnsThumbnailPath()
        {
            var mockThumbnailService = new Mock<IThumbnailService>();
            mockThumbnailService
                .Setup(ts => ts.GenerateThumbnailAsync(It.IsAny<string>()))
                .ReturnsAsync("/thumbnails/test_thumb.jpg");

            var result = await mockThumbnailService.Object.GenerateThumbnailAsync("image/test.jpg");
            Assert.Equal("/thumbnails/test_thumb.jpg", result); 
        }

    }
}
