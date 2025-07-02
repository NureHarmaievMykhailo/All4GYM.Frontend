using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

    public class WorkoutItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
    }

    public class NewWorkoutDto
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        public string? Notes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId == null) return RedirectToPage("/Login");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync("api/Workout");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            Workouts = JsonSerializer.Deserialize<List<WorkoutItem>>(json)!;
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
            JsonSerializer.Serialize(NewWorkout),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("api/Workout", content);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToPage();
        }

        ModelState.AddModelError(string.Empty, "Помилка при створенні тренування");
        return await OnGetAsync();
    }
}
