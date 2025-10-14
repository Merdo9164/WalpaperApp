using Xunit;
using WalpaperApp.Infrastructure.Data;
using WalpaperApp.Infrastructure.Repositories;
using WallpaperApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using System.Reflection;
using System.Linq;

namespace WallpaperApp.Tests.Repositories
{
    public class WallpaperRepositoryTests
    {
        private async Task<AppDbContext> GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "WallpaperTestDb")
            .Options;

            var context = new AppDbContext(options);
            await context.Wallpapers.AddRangeAsync(
                new Wallpaper { Id = 1, Title = "Wallpaper 1", ImageUrl = "url1" },
                new Wallpaper { Id = 2, Title = "Wallpaper 2 ", ImageUrl = "url2" }
            );
            await context.SaveChangesAsync();

            return context;
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllWallpapers()
        {
            //Arrange
            var context = await GetDbContext();
            var repository = new WallpaperRepository(context);

            //Act
            var result = await repository.GetAllAsync();

            //Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }
    }
}