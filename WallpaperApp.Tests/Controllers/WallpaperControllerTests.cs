using System.Net.Http.Json;
using System.Threading.Tasks;
using WalpaperApp.Api;
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

        // POST endpoint testi - hata durumu (örneğin boş title)
        [Fact]
        public async Task PostWallpaper_WithEmptyTitle_ReturnsBadRequest()
        {
            var newWallpaper = new Wallpaper
            {
                Title = "",
                ImageUrl = "test-url"
            };

            var response = await _client.PostAsJsonAsync("/api/wallpaper", newWallpaper);

            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
