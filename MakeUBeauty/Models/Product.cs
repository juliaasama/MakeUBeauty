namespace MakeUBeauty.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int? Discount { get; set; }
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public double Rating { get; set; }
        public string? ColorOptions { get; set; }
        public decimal? OriginalPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}