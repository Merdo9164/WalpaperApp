using System.Net.Http.Json;
using System.Threading.Tasks;
using WallpaperApp.Application.Dtos;
using WallpaperApp.Domain.Entities;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WallpaperApp.Tests.Controllers
{
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

            var wallpapers = await response.Content.ReadFromJsonAsync<Wallpaper[]>();
            Assert.NotNull(wallpapers);
            Assert.NotEmpty(wallpapers); // Test verisi olduğundan emin ol
        }

        // POST endpoint testi
        [Fact]
        public async Task PostWallpaper_WithValidData_ReturnsCreated()
        {
            var newWallpaper = new Wallpaper
            {
                Title = "Test Wallpaper",
                ImageUrl = "test-url"
            };

            var response = await _client.PostAsJsonAsync("/api/wallpaper", newWallpaper);
            response.EnsureSuccessStatusCode();

            var createdWallpaper = await response.Content.ReadFromJsonAsync<Wallpaper>();
            Assert.NotNull(createdWallpaper);
            Assert.Equal("Test Wallpaper", createdWallpaper.Title);
            Assert.Equal("test-url", createdWallpaper.ImageUrl);
        }

        [Fact]
        public async Task PostWallpaper_WithEmptyTitle_ReturnsBadRequest()
        {
            var invalidWallpaper = new WallpaperDto
            {
                Title = "", // Boş title
                ImageUrl = "valid-url"
            };

            var response = await _client.PostAsJsonAsync("/api/wallpaper", invalidWallpaper);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostWallpaper_WithEmptyImageUrl_ReturnsBadRequest()
        {
            var invalidWallpaper = new WallpaperDto
            {
                Title = "Valid Title",
                ImageUrl = "" // Boş imageUrl
            };

            var response = await _client.PostAsJsonAsync("/api/wallpaper", invalidWallpaper);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostWallpaper_WithNullObject_ReturnsBadRequest()
        {
            WallpaperDto? invalidWallpaper = null;

            var response = await _client.PostAsJsonAsync("/api/wallpaper", invalidWallpaper);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetWallpapers_WrongUrl_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/wallpapers-wrong"); // Yanlış endpoint
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetWallpapers_ReturnsListSuccessfully()
        {
            var response = await _client.GetAsync("/api/wallpaper");
            response.EnsureSuccessStatusCode();

            var wallpapers = await response.Content.ReadFromJsonAsync<List<WallpaperDto>>();
            Assert.NotNull(wallpapers);
            Assert.True(wallpapers!.Count >= 0); // boş da olabilir, dolu da
        }

    }
}
