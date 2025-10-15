using Microsoft.EntityFrameworkCore;
using WalpaperApp.Infrastructure.Data;
using WalpaperApp.Infrastructure.Repositories;
using WallpaperApp.Domain.Entities;
using Xunit;
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
            var wallpaper = new Wallpaper { Title = "T", ImageUrl = "u" };
            await repo.AddAsync(wallpaper);

            // Act
            var result = await repo.GetByIdAsync(wallpaper.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(wallpaper.Title, result!.Title);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            var context = CreateInMemoryContext();
            var repo = new WallpaperRepository(context);

            var result = await repo.GetByIdAsync(999);
            Assert.Null(result);
        }
    }
}
