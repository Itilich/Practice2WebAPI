using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Practice_2WebAPI.Data
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

    }
}
