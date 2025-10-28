using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using WallpaperApp.Infrastructure.Data;
using WallpaperApp.Application.Interfaces;
using WallpaperApp.Infrastructure.Repositories;

namespace WallpaperApp.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // 1️⃣ Mevcut DbContextOptions kaydını bul ve kaldır
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // 2️⃣ InMemory database ekle
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                services.AddScoped<IWallpaperRepository, WallpaperRepository>();

                // 3️⃣ Servis sağlayıcısını oluştur ve veritabanını başlat
                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();
                }
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Test sunucusu için base host oluştur
            builder.UseEnvironment("Testing");
            return base.CreateHost(builder);
        }
    }
}
