using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WalpaperApp.Api;
using WalpaperApp.Infrastructure.Data;
using WallpaperApp.Domain.Entities;
using WalpaperApp.Application.Interfaces; // Repository interface
using WalpaperApp.Infrastructure.Repositories; // Repository implementasyonu
using Xunit;

namespace WallpaperApp.Tests.Controllers
{
    public class WallpaperControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public WallpaperControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Gerçek DbContext'i kaldır
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Test için InMemory Db ekle
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));

                    // Servis sağlayıcı üzerinden test verisi ekle
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();

                    db.Wallpapers.AddRange(
                        new Wallpaper { Title = "Test Wallpaper 1", ImageUrl = "url1" },
                        new Wallpaper { Title = "Test Wallpaper 2", ImageUrl = "url2" }
                    );
                    db.SaveChanges();
                });
            }).CreateClient();
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/wallpaper");

            // HTTP 200 OK kontrolü
            response.EnsureSuccessStatusCode();

            // Opsiyonel: JSON veriyi kontrol et
            var wallpapers = await response.Content.ReadFromJsonAsync<List<Wallpaper>>();
            Assert.NotNull(wallpapers);
            Assert.True(wallpapers.Count >= 2);
        }
    }
}

