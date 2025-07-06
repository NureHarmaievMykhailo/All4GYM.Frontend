using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class MealLogModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MealLogModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<MealLogItem> Entries { get; set; } = new();
    public List<FoodItemOption> FoodItems { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? SelectedDate { get; set; } = DateTime.Today;

    [BindProperty(SupportsGet = true)]
    public string? SelectedMealType { get; set; }

    [BindProperty]
    public NewEntryDto NewEntry { get; set; } = new();

    public float TotalCalories => Entries.Sum(e => e.Calories);
    public float TotalProteins => Entries.Sum(e => e.Proteins);
    public float TotalFats => Entries.Sum(e => e.Fats);
    public float TotalCarbs => Entries.Sum(e => e.Carbs);

    public class FoodItemOption
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    public class MealLogItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("mealType")]
        public string MealType { get; set; } = null!;

        [JsonPropertyName("foodItemName")]
        public string FoodItemName { get; set; } = null!;

        [JsonPropertyName("grams")]
        public float Grams { get; set; }

        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonPropertyName("proteins")]
        public float Proteins { get; set; }

        [JsonPropertyName("fats")]
        public float Fats { get; set; }

        [JsonPropertyName("carbs")]
        public float Carbs { get; set; }
    }

    public class NewEntryDto
    {
        [Required]
        public int FoodItemId { get; set; }

        [Required]
        public float Grams { get; set; }

        [Required]
        public string MealType { get; set; } = "Breakfast";
        
        public DateTime Date { get; set; } = DateTime.Today;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId == null) return RedirectToPage("/Login");

        var client = CreateClient();

        var url = "api/MealLog";
        if (SelectedDate != null)
        {
            var dateString = SelectedDate.Value.ToString("yyyy-MM-dd");
            url += $"?date={dateString}";
        }

        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var allEntries = JsonSerializer.Deserialize<List<MealLogItem>>(json)!;

            Entries = string.IsNullOrEmpty(SelectedMealType)
                ? allEntries
                : allEntries.Where(e => e.MealType == SelectedMealType).ToList();
        }

        var foodRes = await client.GetAsync("api/FoodItem");
        if (foodRes.IsSuccessStatusCode)
        {
            var json = await foodRes.Content.ReadAsStringAsync();
            FoodItems = JsonSerializer.Deserialize<List<FoodItemOption>>(json)!;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || UserId == null) return await OnGetAsync();

        // Додати дату, якщо не вказано
        if (SelectedDate != null)
        {
            NewEntry.Date = SelectedDate.Value;
        }
        else
        {
            NewEntry.Date = DateTime.Today;
        }

        var json = JsonSerializer.Serialize(NewEntry);
        Console.WriteLine("📤 Payload: " + json);

        var client = CreateClient();

        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var res = await client.PostAsync("api/MealLog", content);

        if (!res.IsSuccessStatusCode)
        {
            var error = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Error response: {error}");
            ModelState.AddModelError(string.Empty, $"Помилка: {error}");
            return await OnGetAsync(); // Повертаємось на сторінку з помилкою
        }

        return RedirectToPage(new
        {
            date = NewEntry.Date.ToString("yyyy-MM-dd"),
            selectedMealType = SelectedMealType
        });
    }


    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (UserId == null) return RedirectToPage("/Login");

        var client = CreateClient();
        var res = await client.DeleteAsync($"api/MealLog/{id}");
        return RedirectToPage(new { date = SelectedDate?.ToString("yyyy-MM-dd"), selectedMealType = SelectedMealType });
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }
}
