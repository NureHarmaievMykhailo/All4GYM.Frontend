using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class ProfileModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProfileModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    [Required]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Console.WriteLine("📥 GET: /Profile");

        var jwt = Request.Cookies["jwt"];
        Console.WriteLine($"🔐 JWT from cookie: {jwt}");

        if (string.IsNullOrEmpty(jwt))
        {
            Console.WriteLine("❌ JWT not found, redirecting to /Login");
            return RedirectToPage("/Login");
        }

        if (UserId == null)
        {
            Console.WriteLine("❌ UserId not found in BasePageModel");
            return RedirectToPage("/Login");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5092/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync("api/User/profile");
            Console.WriteLine($"🔁 Response status: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"📦 Response content: {json}");

            response.EnsureSuccessStatusCode();

            var user = JsonDocument.Parse(json).RootElement;

            FullName = user.GetProperty("fullName").GetString()!;
            Email = user.GetProperty("email").GetString()!;
            Role = user.GetProperty("role").GetString()!;

            Console.WriteLine($"✅ Profile loaded: {FullName}, {Email}, {Role}");
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не вдалося завантажити профіль: " + ex.Message;
            Console.WriteLine($"💥 Exception: {ex}");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Console.WriteLine("📥 POST: /Profile");

        if (!ModelState.IsValid)
        {
            Console.WriteLine("❌ Model validation failed");
            return Page();
        }

        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt))
        {
            Console.WriteLine("❌ JWT not found, redirecting to /Login");
            return RedirectToPage("/Login");
        }

        if (UserId == null)
        {
            Console.WriteLine("❌ UserId is null, redirecting to /Login");
            return RedirectToPage("/Login");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5092/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var payload = JsonSerializer.Serialize(new
            {
                fullName = FullName,
                email = Email,
                password = "dummy" // як placeholder
            });

            Console.WriteLine($"📤 Updating profile: {payload}");

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PutAsync("api/User/profile", content);
            Console.WriteLine($"🔁 Update response: {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            SuccessMessage = "✅ Профіль успішно оновлено!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "❌ Помилка при оновленні: " + ex.Message;
            Console.WriteLine($"💥 Exception during update: {ex}");
        }

        return Page();
    }
}
