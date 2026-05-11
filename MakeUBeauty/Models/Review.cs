using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MakeUBeauty.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please leave a comment.")]
        [StringLength(1000)]
        public string Comment { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public DateTime DatePosted { get; set; } = DateTime.Now;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public string? UserName { get; set; }
    }
}