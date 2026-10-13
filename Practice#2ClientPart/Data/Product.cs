using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Practice_2ClientPart.Data
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;

    }
}
