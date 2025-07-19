using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class WorkoutDetailsModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WorkoutDetailsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; } // WorkoutId з URL

    public DateTime WorkoutDate { get; set; }
    public string? WorkoutNotes { get; set; }

    public List<ExerciseItem> Exercises { get; set; } = new();
    public List<ExerciseOption> AvailableExercises { get; set; } = new();

    [BindProperty]
    public AddExerciseDto NewExercise { get; set; } = new();

    public class ExerciseItem
    {
        [JsonPropertyName("exerciseId")]
        public int ExerciseId { get; set; }

        [JsonPropertyName("exerciseName")]
        public string ExerciseName { get; set; } = null!;

        [JsonPropertyName("sets")]
        public int Sets { get; set; }

        [JsonPropertyName("reps")]
        public int Reps { get; set; }

        [JsonPropertyName("weight")]
        public float Weight { get; set; }
    }

    public class ExerciseOption
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    public class AddExerciseDto
    {
        [JsonPropertyName("workoutId")]
        public int WorkoutId { get; set; }

        [Required]
        [JsonPropertyName("exerciseId")]
        public int ExerciseId { get; set; }

        [JsonPropertyName("sets")]
        public int Sets { get; set; }

        [JsonPropertyName("reps")]
        public int Reps { get; set; }

        [JsonPropertyName("weight")]
        public float Weight { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
{
    if (UserId == null) return RedirectToPage("/Login");

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

    if (!hasActiveSubscription || !Enum.TryParse<SubscriptionTier>(tierStr, out var tier) || tier < SubscriptionTier.Basic)
    {
        Console.WriteLine("🚫 Access denied: insufficient subscription tier");
        return RedirectToPage("/AccessDenied");
    }
    
    var workoutRes = await client.GetAsync($"api/Workout/{Id}");
    if (workoutRes.IsSuccessStatusCode)
    {
        var json = await workoutRes.Content.ReadAsStringAsync();
        var workout = JsonDocument.Parse(json).RootElement;

        if (workout.TryGetProperty("date", out var dateProp) && dateProp.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(dateProp.GetString(), out var parsedDate))
                WorkoutDate = parsedDate;
        }

        WorkoutNotes = workout.GetProperty("notes").GetString();
    }

    var exercisesRes = await client.GetAsync($"api/workouts/{Id}/exercises");
    if (exercisesRes.IsSuccessStatusCode)
    {
        var json = await exercisesRes.Content.ReadAsStringAsync();
        Exercises = JsonSerializer.Deserialize<List<ExerciseItem>>(json)!;
    }

    // 🔽 Усі доступні вправи
    var optionsRes = await client.GetAsync("api/Exercise");
    if (optionsRes.IsSuccessStatusCode)
    {
        var json = await optionsRes.Content.ReadAsStringAsync();
        AvailableExercises = JsonSerializer.Deserialize<List<ExerciseOption>>(json)!;
    }

    return Page();
}

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return await OnGetAsync();
        if (UserId == null) return RedirectToPage("/Login");

        NewExercise.WorkoutId = Id;

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var content = new StringContent(
            JsonSerializer.Serialize(NewExercise),
            Encoding.UTF8,
            "application/json");

        await client.PostAsync($"api/workouts/{Id}/exercises", content);
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int exerciseId)
    {
        if (UserId == null) return RedirectToPage("/Login");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        await client.DeleteAsync($"api/workouts/{Id}/exercises/{exerciseId}");
        return RedirectToPage(new { Id });
    }
}
