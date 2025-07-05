using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class NutritionCalculatorModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NutritionCalculatorModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public NutritionInput Input { get; set; } = new();

    public NutritionResult? Result { get; set; }

    public class NutritionInput
    {
        [Required]
        [JsonPropertyName("gender")]
        public string Gender { get; set; } = "male";

        [Required]
        [Range(10, 120)]
        [JsonPropertyName("age")]
        public int Age { get; set; }

        [Required]
        [Range(30, 250)]
        [JsonPropertyName("weight")]
        public float Weight { get; set; } // kg

        [Required]
        [Range(100, 250)]
        [JsonPropertyName("height")]
        public float Height { get; set; } // cm

        [Required]
        [JsonPropertyName("activityLevel")]
        public string ActivityLevel { get; set; } = "moderate";

        [Required]
        [JsonPropertyName("goal")]
        public string Goal { get; set; } = "maintain";
    }

    public class NutritionResult
    {
        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonPropertyName("proteinGrams")]
        public float Protein { get; set; }

        [JsonPropertyName("fatGrams")]
        public float Fat { get; set; }

        [JsonPropertyName("carbsGrams")]
        public float Carbs { get; set; }
    }


    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Console.WriteLine("❌ ModelState is invalid.");
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key]?.Errors;
                if (errors != null && errors.Count > 0)
                {
                    Console.WriteLine($"❌ Error in '{key}': {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                }
            }

            return Page();
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var json = JsonSerializer.Serialize(Input);
        Console.WriteLine($"📤 Sending payload: {json}");
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("api/Nutrition/calculate", content);
        Console.WriteLine($"📥 Response status: {res.StatusCode}");

        if (res.IsSuccessStatusCode)
        {
            var responseBody = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"📦 Response body: {responseBody}");
            Result = JsonSerializer.Deserialize<NutritionResult>(responseBody);
        }
        else
        {
            var error = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Failed to calculate nutrition. Server says: {error}");
        }

        return Page();
    }

}
