using System.ComponentModel.DataAnnotations;

namespace Practice_2ClientPart.Models
{
    public class NewProductDto
    {
        [Required(ErrorMessage = "Укажите название товара")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Range(0.01, 1_000_000, ErrorMessage = "Цена должна быть больше нуля")]
        [Display(Name = "Цена")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Выберите категорию")]
        [Display(Name = "Категория")]
        public int CategoryId { get; set; }
    }
}