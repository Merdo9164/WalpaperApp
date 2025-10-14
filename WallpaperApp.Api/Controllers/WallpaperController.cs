using Microsoft.AspNetCore.Mvc;
using WallpaperApp.Application.Dtos;
using WalpaperApp.Application.Interfaces;

namespace WalpaperApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WallpaperController : ControllerBase
    {
        private readonly IWallpaperRepository _repository;

        public WallpaperController(IWallpaperRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WallpaperDto>>> GetWallpapers()
        {
            var wallpapers = await _repository.GetAllAsync();

            var wallpaperDtos = wallpapers.Select(w => new WallpaperDto
            {
                Id = w.Id,
                Title = w.Title,
                ImageUrl = w.ImageUrl
            });

            return Ok(wallpaperDtos);
        }
    }
}
