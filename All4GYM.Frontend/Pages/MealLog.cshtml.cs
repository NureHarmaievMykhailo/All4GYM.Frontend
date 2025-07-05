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

    public List<MealLogItem> Logs { get; set; } = new();

    [BindProperty]
    public NewMealLogDto NewLog { get; set; } = new();

    public class MealLogItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonPropertyName("proteins")]
        public float Proteins { get; set; }

        [JsonPropertyName("fats")]
        public float Fats { get; set; }

        [JsonPropertyName("carbs")]
        public float Carbs { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class NewMealLogDto
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public int Calories { get; set; }

        [Required]
        public float Proteins { get; set; }

        [Required]
        public float Fats { get; set; }

        [Required]
        public float Carbs { get; set; }

        public string? Notes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId == null) return RedirectToPage("/Login");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var res = await client.GetAsync("api/MealLog");
        if (res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync();
            Logs = JsonSerializer.Deserialize<List<MealLogItem>>(json)!;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return await OnGetAsync();
        if (UserId == null) return RedirectToPage("/Login");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var content = new StringContent(JsonSerializer.Serialize(NewLog), Encoding.UTF8, "application/json");
        var res = await client.PostAsync("api/MealLog", content);

        if (res.IsSuccessStatusCode)
        {
            return RedirectToPage();
        }

        ModelState.AddModelError(string.Empty, "Помилка при збереженні запису.");
        return await OnGetAsync();
    }
}
