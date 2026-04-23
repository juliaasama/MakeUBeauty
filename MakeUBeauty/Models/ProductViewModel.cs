using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MakeUBeauty.Models
{
    public class ProductViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int? Discount { get; set; }
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Please upload a product image")]
        public IFormFile imageFile { get; set; } 
    }
}