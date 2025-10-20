using Microsoft.EntityFrameworkCore;
using WalpaperApp.Infrastructure.Data;
using WalpaperApp.Infrastructure.Repositories;
using WallpaperApp.Domain.Entities;
using Xunit;
using System;
using System.Threading.Tasks;

namespace WallpaperApp.Tests.Repository
{
    public class WallpaperRepositoryTests
    {
        private AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsWallpaper_WhenExists()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var repo = new WallpaperRepository(context);
            var wallpaper = new Wallpaper 
            { 
                Title = "T", 
                ImageUrl = "u" 
            };
            await repo.AddAsync(wallpaper);

            // Act
            var result = await repo.GetByIdAsync(wallpaper.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(wallpaper.Title, result!.Title);
            Assert.Equal(wallpaper.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            var context = CreateInMemoryContext();
            var repo = new WallpaperRepository(context);

            // Geçerli olmayan GUID kullanıyoruz
            var nonExistentId = Guid.NewGuid();

            var result = await repo.GetByIdAsync(nonExistentId);
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_AssignsGuidId()
        {
            var context = CreateInMemoryContext();
            var repo = new WallpaperRepository(context);

            var wallpaper = new Wallpaper
            {
                Title = "New Wallpaper",
                ImageUrl = "http://example.com/image.jpg"
            };

            var added = await repo.AddAsync(wallpaper);

            Assert.NotEqual(Guid.Empty, added.Id);
            Assert.Equal("New Wallpaper", added.Title);
            Assert.Equal("http://example.com/image.jpg", added.ImageUrl);
        }
    }
}
