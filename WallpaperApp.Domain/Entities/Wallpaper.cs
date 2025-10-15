namespace WallpaperApp.Domain.Entities
{
    public class Wallpaper
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
