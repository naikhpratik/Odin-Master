﻿using System.Collections.Generic;

namespace Odin.Domain.Bots.Dtos
{
    public class HousingPropertyImagesDto
    {
        public HousingPropertyImagesDto()
        {
            Images = new List<string>();
        }

        public string Id { get; set; }
        public List<string> Images { get; set; }
    }
}