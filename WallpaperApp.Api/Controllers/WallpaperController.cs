using Microsoft.AspNetCore.Mvc;
using WalpaperApp.Application.Interfaces;
using WallpaperApp.Domain.Entities;
using System;
using System.Threading.Tasks;

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

        // GET: api/wallpaper
        [HttpGet]
        public async Task<IActionResult> GetWallpapers()
        {
            var wallpapers = await _repository.GetAllAsync();
            return Ok(wallpapers);
        }

        // GET: api/wallpaper/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetWallpaper(Guid id)
        {
            var wallpaper = await _repository.GetByIdAsync(id);
            if (wallpaper == null)
                return NotFound();

            return Ok(wallpaper);
        }

        // POST: api/wallpaper
        [HttpPost]
        public async Task<IActionResult> PostWallpaper([FromBody] Wallpaper wallpaper)
        {
            if (wallpaper == null)
                return BadRequest("Wallpaper cannot be null.");

            if (string.IsNullOrWhiteSpace(wallpaper.Title))
                return BadRequest("Title cannot be empty.");

            if (string.IsNullOrWhiteSpace(wallpaper.ImageUrl))
                return BadRequest("ImageUrl cannot be empty.");

            wallpaper.Id = Guid.NewGuid();
            await _repository.AddAsync(wallpaper);

            return CreatedAtAction(nameof(GetWallpaper), new { id = wallpaper.Id }, wallpaper);
        }

        // DELETE: api/wallpaper/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteWallpaper(Guid id)
        {
            var wallpaper = await _repository.GetByIdAsync(id);
            if (wallpaper == null)
                return NotFound();

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
