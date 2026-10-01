using System.ComponentModel.DataAnnotations;

namespace Practice_2WebAPI.Data
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }

    }
}
