using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class FoodItemsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FoodItemsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<FoodItemDto> FoodItems { get; set; } = new();

    [BindProperty]
    public CreateFoodItemDto NewFoodItem { get; set; } = new();

    public class FoodItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonPropertyName("proteins")]
        public float Proteins { get; set; }

        [JsonPropertyName("fats")]
        public float Fats { get; set; }

        [JsonPropertyName("carbs")]
        public float Carbs { get; set; }
    }

    public class CreateFoodItemDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonPropertyName("proteins")]
        public float Proteins { get; set; }

        [JsonPropertyName("fats")]
        public float Fats { get; set; }

        [JsonPropertyName("carbs")]
        public float Carbs { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var res = await client.GetAsync("api/FoodItem");
        if (res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync();
            FoodItems = JsonSerializer.Deserialize<List<FoodItemDto>>(json)!;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return await OnGetAsync();

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var content = new StringContent(JsonSerializer.Serialize(NewFoodItem), Encoding.UTF8, "application/json");
        await client.PostAsync("api/FoodItem", content);

        return RedirectToPage();
    }
}
