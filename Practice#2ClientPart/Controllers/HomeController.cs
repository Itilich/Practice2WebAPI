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
        private const string BaseAddress = "https://localhost:7210/api";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        //КАТЕГОРИИ
        [HttpGet]
        public async Task<IActionResult> Index(string? name)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{BaseAddress}/Category");

            List<Category> categories = new();
            if (response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                categories = JsonSerializer.Deserialize<List<Category>>(responseText, jsonOptions)
                             ?? new List<Category>();
            }
            else
            {
                ViewBag.Error = $"Сервер вернул ошибку {(int)response.StatusCode}.";
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                categories = categories
                    .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.SearchName = name;
            return View(categories);
        }

        [HttpGet]
        public IActionResult NewCategory()
        {
            return View(new NewCategoryDto());
        }

        [HttpPost]
        public async Task<IActionResult> NewCategory(NewCategoryDto request)
        {
            if (!ModelState.IsValid)
                return View(request);

            var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync($"{BaseAddress}/Category", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Категория «{request.Name}» добавлена.";
                return RedirectToAction(nameof(Index));
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(nameof(request.Name),
                    $"Категория с именем «{request.Name}» уже существует.");
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Ошибка {response.StatusCode}: {errorText}");
            }

            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{BaseAddress}/Category/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = $"Не удалось загрузить категорию с ID {id}.";
                return RedirectToAction(nameof(Index));
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var category = JsonSerializer.Deserialize<Category>(responseText, jsonOptions);

            if (category is null)
            {
                TempData["Error"] = $"Категория с ID {id} не найдена.";
                return RedirectToAction(nameof(Index));
            }

            var model = new NewCategoryDto
            {
                Name = category.Name,
                Description = category.Description
            };

            ViewBag.OriginalId = id;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, NewCategoryDto request)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.OriginalId = id;
                return View(request);
            }

            var httpClient = new HttpClient();
            var response = await httpClient.PutAsJsonAsync($"{BaseAddress}/Category/{id}", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Категория «{request.Name}» обновлена.";
                return RedirectToAction(nameof(Index));
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(nameof(request.Name),
                    $"Категория с именем «{request.Name}» уже существует.");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"Категория с ID {id} не найдена.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Ошибка {response.StatusCode}: {errorText}");
            }

            ViewBag.OriginalId = id;
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.DeleteAsync($"{BaseAddress}/Category/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Категория удалена.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                TempData["Error"] =
                    "Невозможно удалить категорию — к ней привязаны товары. " +
                    "Сначала удалите или перенесите товары этой категории.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"Категория с ID {id} не найдена.";
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Ошибка {response.StatusCode}: {errorText}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ======================= ТОВАРЫ =======================

        [HttpGet]
        public async Task<IActionResult> Products(string? name, int? categoryId)
        {
            var httpClient = new HttpClient();

            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(name))
                query.Add($"name={Uri.EscapeDataString(name)}");
            if (categoryId.HasValue)
                query.Add($"categoryId={categoryId.Value}");

            var url = $"{BaseAddress}/Product";
            if (query.Any())
                url += "?" + string.Join("&", query);

            List<Product> products = new();

            try
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    products = JsonSerializer.Deserialize<List<Product>>(responseText, jsonOptions)
                               ?? new List<Product>();
                }
                else
                {
                    ViewBag.Error = $"Сервер вернул ошибку {(int)response.StatusCode}.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API недоступен");
                ViewBag.Error = "Сервер недоступен.";
            }

            ViewBag.Categories = await LoadCategoriesAsync();
            ViewBag.SearchName = name;
            ViewBag.SearchCategoryId = categoryId;

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> NewProduct()
        {
            ViewBag.Categories = await LoadCategoriesAsync();
            return View(new NewProductDto());
        }

        [HttpPost]
        public async Task<IActionResult> NewProduct(NewProductDto request)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await LoadCategoriesAsync();
                return View(request);
            }

            var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync($"{BaseAddress}/Product", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Товар «{request.Name}» добавлен.";
                return RedirectToAction(nameof(Products));
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(nameof(request.Name),
                    $"Товар с именем «{request.Name}» уже существует.");
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Ошибка {response.StatusCode}: {errorText}");
            }

            ViewBag.Categories = await LoadCategoriesAsync();
            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{BaseAddress}/Product/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = $"Не удалось загрузить товар с ID {id}.";
                return RedirectToAction(nameof(Products));
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var product = JsonSerializer.Deserialize<Product>(responseText, jsonOptions);

            if (product is null)
            {
                TempData["Error"] = $"Товар с ID {id} не найден.";
                return RedirectToAction(nameof(Products));
            }

            var model = new NewProductDto
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId
            };

            ViewBag.OriginalId = id;
            ViewBag.Categories = await LoadCategoriesAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(int id, NewProductDto request)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.OriginalId = id;
                ViewBag.Categories = await LoadCategoriesAsync();
                return View(request);
            }

            var httpClient = new HttpClient();
            var response = await httpClient.PutAsJsonAsync($"{BaseAddress}/Product/{id}", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Товар «{request.Name}» обновлён.";
                return RedirectToAction(nameof(Products));
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(nameof(request.Name),
                    $"Товар с именем «{request.Name}» уже существует.");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"Товар с ID {id} не найден.";
                return RedirectToAction(nameof(Products));
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Ошибка {response.StatusCode}: {errorText}");
            }

            ViewBag.OriginalId = id;
            ViewBag.Categories = await LoadCategoriesAsync();
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.DeleteAsync($"{BaseAddress}/Product/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Товар удалён.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"Товар с ID {id} не найден.";
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Не удалось удалить товар: {response.StatusCode}. {errorText}";
            }

            return RedirectToAction(nameof(Products));
        }

        // ======================= ВСПОМОГАТЕЛЬНЫЕ =======================

        private async Task<List<Category>> LoadCategoriesAsync()
        {
            var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync($"{BaseAddress}/Category");
                if (!response.IsSuccessStatusCode) return new List<Category>();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Category>>(json, options) ?? new List<Category>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Не удалось загрузить категории");
                return new List<Category>();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}