using Microsoft.AspNetCore.Mvc;
using Practice_2ClientPart.Data;
using Practice_2ClientPart.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Practice_2ClientPart.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://localhost:7210/api/Category");
            var responseText = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var responseData = JsonSerializer.Deserialize<List<Category>>(responseText, jsonOptions);

            return View(responseData);
        }
        [HttpGet]
        public IActionResult NewCategory()
        {
            return View(new NewCategoryDto());
        }
        [HttpPost]
        public async Task<IActionResult> NewCategory(NewCategoryDto request)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync("https://localhost:7210/api/Category", request);

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> Delete(string name)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.DeleteAsync($"https://localhost:7210/api/Category/{Uri.EscapeDataString(name)}");
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                TempData["Error"] =
                    $"Невозможно удалить категорию «{name}» -  к ней привязаны товары. " +
                    $"Сначала удалите или перенесите товары этой категории.";
            }
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
