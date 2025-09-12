﻿using Microsoft.AspNetCore.Identity;

namespace WebArsip.Core.Entities
{
    public class User : IdentityUser<int>
    {
        public string Name { get; set; } = string.Empty;
    }
}
