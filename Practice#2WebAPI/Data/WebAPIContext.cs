using Microsoft.EntityFrameworkCore;

namespace Practice_2WebAPI.Data
{
    public class WebAPIContext: DbContext
    {
        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source = shop.db");
            }
        }
    }
}
