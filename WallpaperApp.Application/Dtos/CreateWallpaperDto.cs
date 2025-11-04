using Microsoft.AspNetCore.Http;

namespace WallpaperApp.Application.Dtos
{
    public class CreateWallpaperDto
    {
        public string Title { get; set; } = string.Empty;
        public IFormFile? Image { get; set; } // form-data ile gelecek dosya

        public List<IFormFile>? Images { get; set; } //Çoklu yükleme
    }
}