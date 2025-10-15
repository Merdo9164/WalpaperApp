using System.Net;
using System.Net.Http.Json;
using Xunit;
using WallpaperApp.Application.Dtos;

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
        public async Task GetById_ShouldReturnWallpaper_WhenExists()
        {
            // Arrange
            var responseAll = await _client.GetAsync("/api/wallpaper");
            responseAll.EnsureSuccessStatusCode();

            var wallpapers = await responseAll.Content.ReadFromJsonAsync<List<WallpaperDto>>();
            var firstId = wallpapers!.First().Id;

            // Act
            var response = await _client.GetAsync($"/api/wallpaper/{firstId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var wallpaper = await response.Content.ReadFromJsonAsync<WallpaperDto>();
            Assert.NotNull(wallpaper);
            Assert.Equal(firstId, wallpaper!.Id);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenNotExists()
        {
            // Act
            var response = await _client.GetAsync("/api/wallpaper/9999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
