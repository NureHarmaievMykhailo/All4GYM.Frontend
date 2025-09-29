using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class ProgressLogModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProgressLogModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<ProgressItem> Logs { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? SelectedDate { get; set; } = DateTime.Today;

    [BindProperty]
    public NewProgressDto NewProgress { get; set; } = new();

    public class ProgressItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("weight")]
        public float Weight { get; set; }

        [JsonPropertyName("bodyFat")]
        public float? BodyFat { get; set; }

        [JsonPropertyName("muscleMass")]
        public float? MuscleMass { get; set; }
    }

    public class NewProgressDto
    {
        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public float Weight { get; set; }

        public float? BodyFat { get; set; }
        public float? MuscleMass { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId == null) return RedirectToPage("/Login");
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt)) return RedirectToPage("/Login");

        var client = CreateClient();

        // Перевірка підписки
        try
        {
            var profileRes = await client.GetAsync("api/User/profile");
            profileRes.EnsureSuccessStatusCode();

            var json = await profileRes.Content.ReadAsStringAsync();
            var profile = JsonDocument.Parse(json).RootElement;

            var tierStr = profile.GetProperty("subscriptionTier").GetString();
            if (!Enum.TryParse<SubscriptionTier>(tierStr, out var tier) || tier < SubscriptionTier.Pro)
            {
                return RedirectToPage("/AccessDenied");
            }
        }
        catch
        {
            return RedirectToPage("/AccessDenied");
        }

        // Отримання даних прогресу
        var url = "api/ProgressLog";
        if (SelectedDate != null)
        {
            url += $"?date={SelectedDate.Value:yyyy-MM-dd}";
        }

        var res = await client.GetAsync(url);
        if (res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync();
            Logs = JsonSerializer.Deserialize<List<ProgressItem>>(json)!;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || UserId == null) return await OnGetAsync();

        var client = CreateClient();
        var json = JsonSerializer.Serialize(NewProgress);
        Console.WriteLine("📤 Progress payload: " + json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await client.PostAsync("api/ProgressLog", content);

        if (!res.IsSuccessStatusCode)
        {
            var error = await res.Content.ReadAsStringAsync();
            Console.WriteLine("❌ Error: " + error);
            ModelState.AddModelError(string.Empty, "Не вдалося зберегти запис.");
            return await OnGetAsync();
        }

        return RedirectToPage(new { date = NewProgress.Date.ToString("yyyy-MM-dd") });
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/"); // твоя API база
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }
}
