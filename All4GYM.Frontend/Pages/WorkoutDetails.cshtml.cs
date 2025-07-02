using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = null!;
        public int Sets { get; set; }
        public int Reps { get; set; }
        public float Weight { get; set; }
    }

    public class ExerciseOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class AddExerciseDto
    {
        [Required]
        public int ExerciseId { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public float Weight { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId == null) return RedirectToPage("/Login");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        // workout details
        var workoutRes = await client.GetAsync($"api/Workout/{Id}");
        if (workoutRes.IsSuccessStatusCode)
        {
            var json = await workoutRes.Content.ReadAsStringAsync();
            var workout = JsonDocument.Parse(json).RootElement;
            WorkoutDate = DateTime.Parse(workout.GetProperty("date").ToString());
            WorkoutNotes = workout.GetProperty("notes").GetString();
        }

        // exercises in workout
        var exercisesRes = await client.GetAsync($"api/workouts/{Id}/exercises");
        if (exercisesRes.IsSuccessStatusCode)
        {
            var json = await exercisesRes.Content.ReadAsStringAsync();
            Exercises = JsonSerializer.Deserialize<List<ExerciseItem>>(json)!;
        }

        // available exercises
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
