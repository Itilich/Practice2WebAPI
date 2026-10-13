using Microsoft.AspNetCore.Mvc;
using Practice_2ClientPart.Data;
using Practice_2ClientPart.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

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
        public async Task<IActionResult> Index(string? name)
        {
            var httpClient = new HttpClient();
            var url = string.IsNullOrWhiteSpace(name)? "https://localhost:7210/api/Category": $"https://localhost:7210/api/Category?name={Uri.EscapeDataString(name)}";
            var response = await httpClient.GetAsync(url);
            List<Category> responseData = new();
            if (response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                responseData = JsonSerializer.Deserialize<List<Category>>(responseText, jsonOptions)
                             ?? new List<Category>();
            }
            else
            {
                ViewBag.Error = $"Сервер вернул ошибку {(int)response.StatusCode}.";
            }
            ViewBag.SearchName = name;
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
        [HttpGet]
        public async Task<IActionResult> EditCategory(string name)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://localhost:7210/api/Category?name={Uri.EscapeDataString(name)}");

            var responseText = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var responseData = JsonSerializer.Deserialize<List<Category>>(responseText, jsonOptions);
            var category = responseData?.FirstOrDefault();
            if (category is null)
            {
                TempData["Error"] = $"Категория «{name}» не найдена.";
                return RedirectToAction(nameof(Index));
            }
            var model = new NewCategoryDto
            {
                Name = category.Name,
                Description = category.Description
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditCategory(string name, NewCategoryDto request)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.PutAsJsonAsync($"https://localhost:7210/api/Category/{Uri.EscapeDataString(name)}",request);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Категория «{request.Name}» успешно обновлена.";
                return RedirectToAction(nameof(Index));
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(nameof(request.Name),
                    $"Категория с именем «{request.Name}» уже существует.");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"Категория «{name}» не найдена.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Ошибка {response.StatusCode}: {errorText}");
            }
            ViewBag.OriginalName = name;
            return View(request);
        }
        [HttpGet]
        public async Task<IActionResult> Products(string? name, string? categoryName)
        {
            var httpClient = new HttpClient();

            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(name))
                query.Add($"name={Uri.EscapeDataString(name)}");
            if (!string.IsNullOrWhiteSpace(categoryName))
                query.Add($"categoryName={Uri.EscapeDataString(categoryName)}");

            var url = "https://localhost:7210/api/Product";
            if (query.Any())
                url += "?" + string.Join("&", query);

            List<Product> products = new();
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
            

            ViewBag.SearchName = name;
            ViewBag.SearchCategory = categoryName;

            return View(products);
        }
        
        [HttpGet]
        public async Task<IActionResult> EditProduct(string name)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(
                $"https://localhost:7210/api/Product?name={Uri.EscapeDataString(name)}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = $"Не удалось загрузить товар «{name}».";
                return RedirectToAction(nameof(Products));
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<Product>>(responseText, jsonOptions);
            var product = list?.FirstOrDefault();

            if (product is null)
            {
                TempData["Error"] = $"Товар «{name}» не найден.";
                return RedirectToAction(nameof(Products));
            }

            var model = new NewProductDto
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryName = product.CategoryName
            };

            ViewBag.OriginalName = name;
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditProduct(string name, NewProductDto request)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.OriginalName = name;
                return View(request);
            }

            var httpClient = new HttpClient();
                var response = await httpClient.PutAsJsonAsync(
                    $"https://localhost:7210/api/Product/{Uri.EscapeDataString(name)}",
                    request);

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
                    TempData["Error"] = $"Товар «{name}» не найден.";
                    return RedirectToAction(nameof(Products));
                }
                else
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Ошибка {response.StatusCode}: {errorText}");
                }

            ViewBag.OriginalName = name;
            return View(request);
        }
        // === УДАЛЕНИЕ ===
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string name)
        {
            var httpClient = new HttpClient();
                var response = await httpClient.DeleteAsync(
                    $"https://localhost:7210/api/Product/{Uri.EscapeDataString(name)}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"Товар «{name}» удалён.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = $"Товар «{name}» не найден.";
                }
                else
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Не удалось удалить товар: {response.StatusCode}. {errorText}";
                }

            return RedirectToAction(nameof(Products));
        }
        [HttpGet]
        public IActionResult NewProduct()
        {
            return View(new NewProductDto());
        }

        [HttpPost]
        public async Task<IActionResult> NewProduct(NewProductDto request)
        {
            if (!ModelState.IsValid)
                return View(request);

            var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(
                    "https://localhost:7210/api/Product", request);

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
            return View(request);
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
