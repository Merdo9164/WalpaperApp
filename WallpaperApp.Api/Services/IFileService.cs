using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WallpaperApp.Api.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Formdan yüklenen bir resmi sunucuya kaydeder ve URL’sini döner.
        /// </summary>
        Task<string> UploadAsync(IFormFile file ,string title);

        /// <summary>
        /// URL’den resmi indirir, doğrular ve sunucuya kaydeder. 
        /// Kaydedilen dosyanın URL’sini döner.
        /// </summary>
        Task<string> DownloadImageFromUrlAndSaveAsync(string imageUrl);
        Task DeleteFileAsync(string? imageUrl);
        Task<(string imageUrl, string thumbnailUrl)> UploadWithThumbnailAsync(IFormFile file, string title);
        Task<(string imageUrl, string thumbnailUrl)> DownloadImageAndCreateThumbnailAsync(string imageUrl, string title);
        
        Task<List<(string ImageUrl, string ThumbnailUrl)>> UploadMultipleWithThumbnailAsync(List<IFormFile> files, string title);
        

    }
}
