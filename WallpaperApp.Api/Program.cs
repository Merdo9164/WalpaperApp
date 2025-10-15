using Microsoft.EntityFrameworkCore;
using WalpaperApp.Infrastructure.Data;
using WalpaperApp.Application.Interfaces;
using WalpaperApp.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 Veritabanı bağlantısı (InMemory kullanıyoruz)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("WallpaperDb"));

// 🔹 Repository kayıtları
builder.Services.AddScoped<IWallpaperRepository, WallpaperRepository>();

var app = builder.Build();

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

// ✅ Bu satır testler için zorunludur
public partial class Program { }
