using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practice_2WebAPI.Data;
using Practice_2WebAPI.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace Practice_2WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly WebAPIContext _context;
        public CategoryController(WebAPIContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Список категорий",
    Description = "Без параметров возвращает все категории. С параметром name — фильтрует по точному совпадению.")]
        public async Task<ActionResult<IEnumerable<Category>>> GetAll([FromQuery] string? name)
        {
            var query = _context.Categories
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(c => c.Name == name);

            return Ok(await query.ToListAsync());
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

            return CreatedAtAction(nameof(GetAll), new { name = category.Name }, category);
        }
        [HttpPut("{name}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Update(string name, [FromBody] CategoryCreateDto dto)
        {
            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Name == name);
            if (existing is null) return NotFound();

            if (!string.Equals(existing.Name, dto.Name, StringComparison.Ordinal) &&
                await _context.Categories.AnyAsync(c => c.Name == dto.Name))
                return Conflict($"Категория с именем '{dto.Name}' уже существует.");

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpPatch("{name}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Patch(string name, [FromBody] JsonPatchDocument<Category> patchDoc)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == name);
            if (category is null) return NotFound();

            patchDoc.ApplyTo(category, ModelState);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("{name}")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Удалить категорию")]
        public async Task<IActionResult> Delete(string name)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Name == name);

            if (category is null) return NotFound();

            if (category.Products.Any())
                return Conflict("Нельзя удалить категорию, к которой привязаны товары.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
