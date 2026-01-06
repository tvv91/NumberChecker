using System.ComponentModel.DataAnnotations;

namespace VodafoneNumberChecker.Models
{
    public class PropositionType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        public int Count { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
