using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace WallpaperApp.Application.Dtos
{
    public class UpdateWallpaperDto
    {
        public string Title { get; set; } = string.Empty;

    }
}