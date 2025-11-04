using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace WallpaperApp.Application.Dtos
{
    public class UpdateWallpaperDto
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }

        public IFormFile? Image { get; set; }

        public List<IFormFile>? Images{ get; set; }

    }
}