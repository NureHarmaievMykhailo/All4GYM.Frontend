using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class FoodItemModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FoodItemModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public FoodItemInput Input { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public class FoodItemInput
    {
        [Required(ErrorMessage = "Вкажіть назву")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть калорійність")]
        [Range(0, 900, ErrorMessage = "Калорійність має бути від 0 до 900 ккал")]
        public int Calories { get; set; }

        [Required(ErrorMessage = "Вкажіть кількість білків")]
        [Range(0, 100, ErrorMessage = "Білків не може бути більше 100 г")]
        public float Proteins { get; set; }

        [Required(ErrorMessage = "Вкажіть кількість жирів")]
        [Range(0, 100, ErrorMessage = "Жирів не може бути більше 100 г")]
        public float Fats { get; set; }

        [Required(ErrorMessage = "Вкажіть кількість вуглеводів")]
        [Range(0, 100, ErrorMessage = "Вуглеводів не може бути більше 100 г")]
        public float Carbs { get; set; }
    }
    
    public async Task<IActionResult> OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
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

        if (!hasActiveSubscription || !Enum.TryParse<SubscriptionTier>(tierStr, out var tier) || tier < SubscriptionTier.Pro)
        {
            Console.WriteLine("🚫 Access denied: Pro tier required");
            return RedirectToPage("/AccessDenied");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        // Перевірка чи існує вже такий продукт
        var checkResponse = await client.GetAsync("api/FoodItem");
        if (checkResponse.IsSuccessStatusCode)
        {
            var existingJson = await checkResponse.Content.ReadAsStringAsync();
            var existingItems = JsonSerializer.Deserialize<List<FoodItemInput>>(existingJson);
            if (existingItems?.Any(x => x.Name.Equals(Input.Name, StringComparison.OrdinalIgnoreCase)) == true)
            {
                ModelState.AddModelError(string.Empty, "Такий продукт уже існує.");
                return Page();
            }
        }

        var payload = new StringContent(JsonSerializer.Serialize(Input), Encoding.UTF8, "application/json");
        var res = await client.PostAsync("api/FoodItem", payload);

        if (res.IsSuccessStatusCode)
        {
            Message = "Продукт успішно додано.";
            return RedirectToPage("/MealLog");
        }

        ModelState.AddModelError(string.Empty, "Не вдалося створити продукт.");
        return Page();
    }
}
