using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class GroupSessionsModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GroupSessionsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<SessionItem> Sessions { get; set; } = new();
    public List<int> UserSessionIds { get; set; } = new(); // ID сесій, на які записаний користувач

    public string? ErrorMessage { get; set; }
    public class SessionItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = null!;

        [JsonPropertyName("gymName")]
        public string GymName { get; set; } = null!;

        [JsonPropertyName("trainerName")]
        public string TrainerName { get; set; } = null!;

        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("maxParticipants")]
        public int MaxParticipants { get; set; }

        [JsonPropertyName("currentParticipants")]
        public int CurrentParticipants { get; set; }
    }

    public class BookingItem
    {
        [JsonPropertyName("groupSessionId")]
        public int GroupSessionId { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        // всі сесії
        var res = await client.GetAsync("api/GroupSession");
        var json = await res.Content.ReadAsStringAsync();
        Console.WriteLine($"📥 GET /GroupSession → {res.StatusCode}");
        Console.WriteLine($"📦 JSON Response: {json}");

        if (res.IsSuccessStatusCode)
        {
            Sessions = JsonSerializer.Deserialize<List<SessionItem>>(json)!;
            Console.WriteLine($"✅ Parsed GroupSessions: {Sessions.Count}");
        }

        // записи користувача
        var bookingsRes = await client.GetAsync("api/Booking");
        if (bookingsRes.IsSuccessStatusCode)
        {
            var bookingsJson = await bookingsRes.Content.ReadAsStringAsync();
            var bookings = JsonSerializer.Deserialize<List<BookingItem>>(bookingsJson)!;
            UserSessionIds = bookings.Select(b => b.GroupSessionId).ToList();
        }

        return Page();
    }

    [BindProperty(SupportsGet = true, Name = "sessionId")]
    public int SessionId { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UserId == null || SessionId == 0)
            return RedirectToPage("/Login");

        Console.WriteLine($"📤 Booking session ID: {SessionId}");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var bookingDto = new { SessionId = SessionId };
        var content = new StringContent(JsonSerializer.Serialize(bookingDto), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("api/Booking", content);
        Console.WriteLine($"📥 Booking response: {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Booking failed: {error}");
            TempData["ErrorMessage"] = error; // тільки тут, один раз
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int sessionId)
    {
        if (UserId == null) return RedirectToPage("/Login");

        Console.WriteLine($"📤 Cancel booking for session ID: {sessionId}");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.DeleteAsync($"api/Booking/{sessionId}");
        Console.WriteLine($"📥 Cancel response: {response.StatusCode}");

        return RedirectToPage();
    }

}
