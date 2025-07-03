using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class TrainingLibraryModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TrainingLibraryModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<ProgramItem> Programs { get; set; } = new();
    public List<VideoItem> Videos { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? Category { get; set; }

    public class ProgramItem
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; } = null!;

        [JsonPropertyName("description")] public string? Description { get; set; }
    }

    public class VideoItem
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("title")] public string Title { get; set; } = null!;

        [JsonPropertyName("url")] public string Url { get; set; } = null!;

        [JsonPropertyName("duration")] public int Duration { get; set; }

        [JsonPropertyName("category")] public string? Category { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");

        var jwt = Request.Cookies["jwt"];
        if (!string.IsNullOrEmpty(jwt))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            Console.WriteLine($"🔐 JWT added: {jwt.Substring(0, 20)}..."); // обрізаємо для безпеки
        }
        else
        {
            Console.WriteLine("❌ JWT not found in cookies");
        }

        // 🔽 Програми тренувань
        var programRes = await client.GetAsync("api/TrainingProgram");
        Console.WriteLine($"📥 GET TrainingProgram → Status: {programRes.StatusCode}");

        if (programRes.IsSuccessStatusCode)
        {
            var json = await programRes.Content.ReadAsStringAsync();
            Console.WriteLine($"📦 TrainingProgram JSON: {json}");

            try
            {
                Programs = JsonSerializer.Deserialize<List<ProgramItem>>(json)!;
                Console.WriteLine($"✅ Programs loaded: {Programs.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to deserialize programs: {ex.Message}");
            }
        }

        // 🔽 Відео
        var videoRes = await client.GetAsync("api/VideoContent");
        Console.WriteLine($"📥 GET VideoContent → Status: {videoRes.StatusCode}");

        if (videoRes.IsSuccessStatusCode)
        {
            var json = await videoRes.Content.ReadAsStringAsync();
            Console.WriteLine($"📦 VideoContent JSON: {json}");

            try
            {
                var allVideos = JsonSerializer.Deserialize<List<VideoItem>>(json)!;
                Videos = string.IsNullOrEmpty(Category)
                    ? allVideos
                    : allVideos.Where(v => v.Category?.Equals(Category, StringComparison.OrdinalIgnoreCase) == true)
                        .ToList();

                Console.WriteLine($"✅ Videos loaded: {Videos.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to deserialize videos: {ex.Message}");
            }
        }
        else if (videoRes.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("🚫 VideoContent request was unauthorized. Check role or JWT.");
        }

        return Page();
    }
}