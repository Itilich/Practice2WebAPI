using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practice_2WebAPI.Data;
using Practice_2WebAPI.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace Practice_2WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductController : ControllerBase
    {
        private readonly WebAPIContext _context;

        public ProductController(WebAPIContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductReadDto>>> GetAll(
            [FromQuery] string? name,
            [FromQuery] string? categoryName,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageSize > 50) pageSize = 50;
            if (page < 1) page = 1;

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(p => p.Name == name);

            if (!string.IsNullOrWhiteSpace(categoryName))
                query = query.Where(p => p.Category!.Name == categoryName);

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
                    CategoryName = p.Category!.Name
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Создать товар. Категория указывается по имени.
        /// </summary>
        [HttpPost]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Создать товар")]
        public async Task<ActionResult<Product>> Create([FromBody] ProductCreateDto dto)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == dto.CategoryName);

            if (category is null)
                return BadRequest($"Категория '{dto.CategoryName}' не существует.");

            if (await _context.Products.AnyAsync(p => p.Name == dto.Name))
                return Conflict($"Товар с именем '{dto.Name}' уже существует.");

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = category.Id
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { name = product.Name }, product);
        }

        /// <summary>
        /// Полностью обновить товар по имени.
        /// </summary>
        [HttpPut("{name}")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Обновить товар")]
        public async Task<IActionResult> Update(string name, [FromBody] ProductCreateDto dto)
        {
            var existing = await _context.Products.FirstOrDefaultAsync(p => p.Name == name);
            if (existing is null) return NotFound();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == dto.CategoryName);
            if (category is null)
                return BadRequest($"Категория '{dto.CategoryName}' не существует.");

            // Если имя меняется — проверяем уникальность нового имени
            if (!string.Equals(existing.Name, dto.Name, StringComparison.Ordinal) &&
                await _context.Products.AnyAsync(p => p.Name == dto.Name))
                return Conflict($"Товар с именем '{dto.Name}' уже существует.");

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.Price = dto.Price;
            existing.CategoryId = category.Id;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Частично обновить товар по имени.
        /// </summary>
        [HttpPatch("{name}")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Частично обновить товар")]
        public async Task<IActionResult> Patch(string name, [FromBody] JsonPatchDocument<Product> patchDoc)
        {
            if (patchDoc is null) return BadRequest("Тело запроса не может быть пустым.");

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Name == name);
            if (product is null) return NotFound();

            patchDoc.ApplyTo(product, ModelState);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Удалить товар по имени.
        /// </summary>
        [HttpDelete("{name}")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Удалить товар")]
        public async Task<IActionResult> Delete(string name)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Name == name);
            if (product is null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}