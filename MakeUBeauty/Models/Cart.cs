using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MakeUBeauty.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; }

        public string ProductImageUrl { get; set; }

        public string BrandName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        public bool IsSelected { get; set; } = true;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}