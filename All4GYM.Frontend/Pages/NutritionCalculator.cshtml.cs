using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using All4GYM.Frontend.Helpers;
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
        public string Gender { get; set; } = "Male";

        [Required]
        [Range(10, 120)]
        [JsonPropertyName("age")]
        public int Age { get; set; }

        [Required]
        [Range(100, 250)]
        [JsonPropertyName("heightCm")]
        public float Height { get; set; } 
        
        [Required]
        [Range(30, 250)]
        [JsonPropertyName("weightKg")]
        public float Weight { get; set; } 

        [Required]
        [JsonPropertyName("activityLevel")]
        public string ActivityLevel { get; set; } = "ModeratelyActive";

        [Required]
        [JsonPropertyName("goal")]
        public string Goal { get; set; } = "Maintain";
    }

    public class NutritionResult
    {
        [JsonPropertyName("bmr")]
        public int Bmr { get; set; }

        [JsonPropertyName("tdee")]
        public int Tdee { get; set; }

        [JsonPropertyName("targetCalories")]
        public int Calories { get; set; }

        [JsonPropertyName("bmi")]
        public double Bmi { get; set; }

        [JsonPropertyName("bmiStatus")]
        public string BmiStatus { get; set; } = string.Empty;

        [JsonPropertyName("healthyWeightMin")]
        public double HealthyWeightMin { get; set; }

        [JsonPropertyName("healthyWeightMax")]
        public double HealthyWeightMax { get; set; }

        [JsonPropertyName("weightDifference")]
        public double WeightDifference { get; set; }

        [JsonPropertyName("targetProteins")]
        public float Protein { get; set; }

        [JsonPropertyName("targetFats")]
        public float Fat { get; set; }

        [JsonPropertyName("targetCarbs")]
        public float Carbs { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("ApiClient");
        var jwt = Request.Cookies["jwt"];

        if (string.IsNullOrEmpty(jwt))
        {
            Console.WriteLine("❌ JWT not found");
            return RedirectToPage("/Login");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var profileRes = await client.GetAsync("api/User/profile");
        if (!profileRes.IsSuccessStatusCode)
        {
            Console.WriteLine("❌ Failed to fetch profile");
            return RedirectToPage("/AccessDenied");
        }

        var profileJson = await profileRes.Content.ReadAsStringAsync();
        using var profileDoc = JsonDocument.Parse(profileJson);
        var root = profileDoc.RootElement;

        var hasActiveSubscription = root.GetProperty("hasActiveSubscription").GetBoolean();
        var tierStr = root.GetProperty("subscriptionTier").GetString();

        if (!hasActiveSubscription || !Enum.TryParse<SubscriptionTier>(tierStr, out var tier) || tier < SubscriptionTier.Basic)
        {
            Console.WriteLine("🚫 Access denied: Basic subscription required");
            return RedirectToPage("/AccessDenied");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Console.WriteLine("❌ ModelState is invalid.");
            return Page();
        }

        var client = _httpClientFactory.CreateClient("ApiClient");
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