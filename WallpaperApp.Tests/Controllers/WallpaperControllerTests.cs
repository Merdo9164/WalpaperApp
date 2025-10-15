using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using WallpaperApp.Application.Dtos; // DTO
using WalpaperApp.Api;

namespace WallpaperApp.Tests.Controllers
{
    public class WallpaperControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public WallpaperControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetWallpapers_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/wallpaper");
            response.EnsureSuccessStatusCode();

        }

        [Fact]
        public async Task PostWallpaper_ReturnsCreated()
        {
            var newWallpaper = new WallpaperDto
            {
                Title = "Yeni Wallpaper",
                ImageUrl = "yeni-url"
            };

            var response = await _client.PostAsJsonAsync("/api/wallpaper", newWallpaper);
            response.EnsureSuccessStatusCode();

            var createdWallpaper = await response.Content.ReadFromJsonAsync<WallpaperDto>();
            Assert.NotNull(createdWallpaper);
            Assert.Equal("Yeni Wallpaper", createdWallpaper.Title);
        }
    }
}
