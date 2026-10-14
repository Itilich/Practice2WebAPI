using System.ComponentModel.DataAnnotations;

namespace Practice_2WebAPI.DTOs
{
    public class ProductCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int CategoryId { get; set; }
    }
}
