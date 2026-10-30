using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practice_2WebAPI.Data;
using Practice_2WebAPI.DTOs;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Practice_2WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly WebAPIContext _context;

        public ProductController(WebAPIContext context)
        {
            _context = context;
        }

        /// <summary>Список товаров или один товар по ID.</summary>
        [HttpGet]
        [HttpGet("{id:int}")]
        [SwaggerOperation(
            Summary = "Товары",
            Description = "Без id — список с фильтрацией. С id — конкретный товар.")]
        [ProducesResponseType(typeof(IEnumerable<ProductReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProductReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(
            int? id,
            [FromQuery] string? name,
            [FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (id.HasValue)
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id.Value);

                if (product is null) return NotFound();

                return Ok(new ProductReadDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    CategoryId = product.CategoryId,
                    CategoryName = product.Category?.Name ?? string.Empty
                });
            }

            if (pageSize > 50) pageSize = 50;
            if (page < 1) page = 1;

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(p => p.Name == name);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

            var items = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductReadDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category!.Name
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Создать товар")]
        public async Task<ActionResult<Product>> Create([FromBody] ProductCreateDto dto)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return BadRequest($"Категория с ID {dto.CategoryId} не существует.");

            if (await _context.Products.AnyAsync(p => p.Name == dto.Name))
                return Conflict($"Товар с именем '{dto.Name}' уже существует.");

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Обновить товар")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductCreateDto dto)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing is null) return NotFound();

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return BadRequest($"Категория с ID {dto.CategoryId} не существует.");

            if (!string.Equals(existing.Name, dto.Name, StringComparison.Ordinal) &&
                await _context.Products.AnyAsync(p => p.Name == dto.Name))
                return Conflict($"Товар с именем '{dto.Name}' уже существует.");

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.Price = dto.Price;
            existing.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Удалить товар")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
