using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WalpaperApp.Api.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Formdan yüklenen bir resmi sunucuya kaydeder ve URL’sini döner.
        /// </summary>
        Task<string> UploadAsync(IFormFile file);

        /// <summary>
        /// URL’den resmi indirir, doğrular ve sunucuya kaydeder. 
        /// Kaydedilen dosyanın URL’sini döner.
        /// </summary>
        Task<string> DownloadImageFromUrlAndSaveAsync(string imageUrl);
        Task<string> DownloadImageFromUrlAndSaveAsync(string ımageUrl, string v);
    }
}
