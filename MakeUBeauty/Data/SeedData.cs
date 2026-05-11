using Microsoft.EntityFrameworkCore;
using MakeUBeauty.Models;
using MakeUBeauty.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace MakeUBeauty.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>()))
            {
                if (context.Products.Any()) return;

                for (int i = 1; i <= 15; i++)
                {
                    bool isMascara = i > 8;
                    context.Products.Add(new Product
                    {
                        Name = isMascara ? $"Maybelline Sky High {i}" : $"Maybelline Luxe {i}",
                        Brand = "Maybelline",
                        Category = isMascara ? "Mascara" : "Lipstick",
                        Price = 400m + (i * 10m),
                        OldPrice = 550m + (i * 10m),
                        Rating = 4.8,
                        ImageUrl = isMascara ? "mascara.jpg" : "lipstick.jpg",
                        IsActive = true
                    });
                }

                for (int i = 1; i <= 15; i++)
                {
                    bool isEyeshadow = i > 8;
                    context.Products.Add(new Product
                    {
                        Name = isEyeshadow ? $"Issy Shadow Palette {i}" : $"Issy Skin Booster {i}",
                        Brand = "Issy",
                        Category = isEyeshadow ? "Eyeshadow" : "Foundation",
                        Price = 500m + (i * 5m),
                        OldPrice = 600m + (i * 5m),
                        Rating = 4.7,
                        ImageUrl = isEyeshadow ? "eyeshadow.jpg" : "foundation.jpg",
                        ColorOptions = "Fair, Natural, Tan",
                        IsActive = true
                    });
                }

                for (int i = 1; i <= 15; i++)
                {
                    context.Products.Add(new Product
                    {
                        Name = $"Vice Ganda Glow {i}",
                        Brand = "Vice Cosmetics",
                        Category = "Blush",
                        Price = 250m + (i * 8m),
                        OldPrice = 350m + (i * 8m),
                        Rating = 4.5,
                        ImageUrl = "blush.jpg",
                        IsActive = true
                    });
                }

                context.SaveChanges();
            }
        }
    }
}