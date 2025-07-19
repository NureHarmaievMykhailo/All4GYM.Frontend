using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class HealthyRecipesModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HealthyRecipesModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<RecipeItem> Recipes { get; set; } = new();

    public class RecipeItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("calories")]
        public int Calories { get; set; }
        
        [JsonPropertyName("proteins")]
        public float Proteins { get; set; }
        
        [JsonPropertyName("fats")]
        public float Fats { get; set; }
        
        [JsonPropertyName("carbs")]
        public float Carbs { get; set; }
        
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var res = await client.GetAsync("api/Recipe");
        var json = await res.Content.ReadAsStringAsync();

        Console.WriteLine($"📦 Recipes JSON: {json}");

        if (res.IsSuccessStatusCode)
        {
            try
            {
                Recipes = JsonSerializer.Deserialize<List<RecipeItem>>(json)!;
                Console.WriteLine($"✅ Loaded {Recipes.Count} recipes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Deserialization error: {ex.Message}");
            }
        }

        return Page();
    }

}