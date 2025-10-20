namespace WallpaperApp.Domain.Entities
{
    public class Wallpaper
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
