using Microsoft.EntityFrameworkCore;
using WalpaperApp.Infrastructure.Data;
using WalpaperApp.Infrastructure.Repositories;
using WalpaperApp.Application.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext: SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IWallpaperRepository, WallpaperRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Partial class for WebApplicationFactory in tests
public partial class Program { }
