namespace MakeUBeauty.Models
{

    public class Cart

    {

        public int Id { get; set; }

        public int UserId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductImageUrl { get; set; }

        public string BrandName { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public bool IsSelected { get; set; } = true;

    }
}