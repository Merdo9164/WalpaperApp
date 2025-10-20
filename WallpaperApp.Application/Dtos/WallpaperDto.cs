namespace WallpaperApp.Application.Dtos
{
    public class WallpaperDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}