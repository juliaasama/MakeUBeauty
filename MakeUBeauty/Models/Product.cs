using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MakeUBeauty.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Brand { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 100000, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public int? Discount { get; set; }

        public double Rating { get; set; }

        public string? ColorOptions { get; set; }

        public bool IsActive { get; set; } = true;

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}