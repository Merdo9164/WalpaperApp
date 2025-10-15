using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WallpaperApp.Domain.Entities;
using WalpaperApp.Infrastructure.Data;
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
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Add InMemory DbContext
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));

                    // Seed data
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Wallpapers.Add(new Wallpaper { Id = 1, Title = "Test Wallpaper", ImageUrl = "http://example.com/test.jpg" });
                    db.SaveChanges();
                });
            }).CreateClient();
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/wallpaper");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var wallpapers = await response.Content.ReadFromJsonAsync<List<Wallpaper>>();
            Assert.Single(wallpapers);
            //denemeeeeee
        }
    }
}
