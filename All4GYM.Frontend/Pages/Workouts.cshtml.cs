using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class WorkoutsModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WorkoutsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<WorkoutItem> Workouts { get; set; } = new();

    [BindProperty]
    public NewWorkoutDto NewWorkout { get; set; } = new();

    public List<TrainingProgramItem> AvailablePrograms { get; set; } = new();

    public class TrainingProgramItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class WorkoutItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class NewWorkoutDto
    {
        [Required(ErrorMessage = "Оберіть програму")]
        [JsonPropertyName("trainingProgramId")]
        public int TrainingProgramId { get; set; }

        [Required(ErrorMessage = "Оберіть дату")]
        [DataType(DataType.Date)]
        [JsonPropertyName("date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId == null)
            return RedirectToPage("/Login");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        try
        {
            var profileResponse = await client.GetAsync("api/User/profile");
            if (!profileResponse.IsSuccessStatusCode)
                return RedirectToPage("/AccessDenied");

            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            using var profileDoc = JsonDocument.Parse(profileJson);
            var root = profileDoc.RootElement;

            var hasActiveSubscription = root.GetProperty("hasActiveSubscription").GetBoolean();
            var tierStr = root.GetProperty("subscriptionTier").GetString();

            if (!hasActiveSubscription || !Enum.TryParse<SubscriptionTier>(tierStr, out var tier) || tier < SubscriptionTier.Basic)
            {
                return RedirectToPage("/AccessDenied");
            }

            var response = await client.GetAsync("api/Workout");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Workouts = JsonSerializer.Deserialize<List<WorkoutItem>>(json)!;
            }

            var programsRes = await client.GetAsync("api/TrainingProgram");
            if (programsRes.IsSuccessStatusCode)
            {
                var json = await programsRes.Content.ReadAsStringAsync();
                AvailablePrograms = JsonSerializer.Deserialize<List<TrainingProgramItem>>(json)!;
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Не вдалося завантажити дані: {ex.Message}");
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

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(NewWorkout),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("api/Workout", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var created = JsonDocument.Parse(json).RootElement;

                if (created.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var newId))
                {
                    return RedirectToPage("/WorkoutDetails", new { id = newId });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Створено тренування, але не вдалося отримати ID.");
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Помилка при створенні тренування: {response.StatusCode} — {error}");
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Внутрішня помилка: {ex.Message}");
        }

        return await OnGetAsync();
    }
}
