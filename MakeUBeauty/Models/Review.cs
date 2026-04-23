using System;
using System.ComponentModel.DataAnnotations;

namespace MakeUBeauty.Models
{
    public class Review
    {
        public int Id { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateTime DatePosted { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public string? UserName { get; set; }

    }
}