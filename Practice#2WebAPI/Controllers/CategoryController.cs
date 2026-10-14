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
    public class CategoryController : ControllerBase
    {
        private readonly WebAPIContext _context;

        public CategoryController(WebAPIContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet("{id:int}")]
        [SwaggerOperation(
    Summary = "Категории",
    Description = "Без id — все категории. С id — конкретная категория.")]
        [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(int? id)
        {
            if (id.HasValue)
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id.Value);

                return category is null ? NotFound() : Ok(category);
            }

            return Ok(await _context.Categories
                .Include(c => c.Products)
                .ToListAsync());
        }

        [HttpPost]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Создать категорию")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Category>> Create([FromBody] CategoryCreateDto dto)
        {
            if (await _context.Categories.AnyAsync(c => c.Name == dto.Name))
                return Conflict($"Категория с именем '{dto.Name}' уже существует.");

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
        }

        [HttpPut("{id:int}")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Обновить категорию")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryCreateDto dto)
        {
            var existing = await _context.Categories.FindAsync(id);
            if (existing is null) return NotFound();

            if (!string.Equals(existing.Name, dto.Name, StringComparison.Ordinal) &&
                await _context.Categories.AnyAsync(c => c.Name == dto.Name))
                return Conflict($"Категория с именем '{dto.Name}' уже существует.");

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [SwaggerOperation(Summary = "Удалить категорию")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category is null) return NotFound();

            if (category.Products.Any())
                return Conflict("Нельзя удалить категорию, к которой привязаны товары.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}