using Microsoft.EntityFrameworkCore;
using WalpaperApp.Infrastructure.Data;
using WalpaperApp.Application.Interfaces;
using WalpaperApp.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);



builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,           // Maksimum deneme sayısı
            maxRetryDelay: TimeSpan.FromSeconds(10), // Denemeler arası bekleme süresi
            errorNumbersToAdd: null)    // Özel SQL hatalarını eklemek için
    )
);
}



// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 Veritabanı bağlantısı (InMemory kullanıyoruz)
//builder.Services.AddDbContext<AppDbContext>(options =>
// options.UseInMemoryDatabase("WallpaperDb"));

// 🔹 Repository kayıtları
builder.Services.AddScoped<IWallpaperRepository, WallpaperRepository>();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // Tablolar yoksa oluştur
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

//Bu satır testler için zorunludur
public partial class Program { }
