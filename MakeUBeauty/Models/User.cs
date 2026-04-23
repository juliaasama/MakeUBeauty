namespace MakeUBeauty.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
        public DateTime? Birthday { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}