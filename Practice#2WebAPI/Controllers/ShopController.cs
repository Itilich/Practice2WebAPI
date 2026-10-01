using Microsoft.AspNetCore.Mvc;
using Practice_2WebAPI.Data;

namespace Practice_2WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShopController : ControllerBase
    {
       
        private readonly ILogger<ShopController> _logger;

        private readonly WebAPIContext _context;

        public ShopController(ILogger<ShopController> logger, WebAPIContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return _context.Products.ToList();

        }
    }
}
