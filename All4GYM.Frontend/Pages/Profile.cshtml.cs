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

    public bool HasActiveSubscription { get; set; }
    public string SubscriptionTier { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt) || UserId == null)
            return RedirectToPage("/Login");

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5092/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync("api/User/profile");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var user = JsonDocument.Parse(json).RootElement;

            FullName = user.GetProperty("fullName").GetString()!;
            Email = user.GetProperty("email").GetString()!;
            Role = user.GetProperty("role").GetString()!;
            HasActiveSubscription = user.GetProperty("hasActiveSubscription").GetBoolean();
            SubscriptionTier = user.GetProperty("subscriptionTier").GetString()!;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не вдалося завантажити профіль: " + ex.Message;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(Request.Cookies["jwt"]) || UserId == null)
            return RedirectToPage("/Login");

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5092/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["jwt"]);

            var payload = JsonSerializer.Serialize(new
            {
                fullName = FullName,
                email = Email,
                password = "dummy"
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PutAsync("api/User/profile", content);
            response.EnsureSuccessStatusCode();

            SuccessMessage = "✅ Профіль успішно оновлено!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "❌ Помилка при оновленні: " + ex.Message;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelSubscriptionAsync()
    {
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt))
            return RedirectToPage("/Login");

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5092/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsync("api/Subscription/cancel", null);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json).RootElement;

            if (doc.TryGetProperty("token", out var newToken))
            {
                Response.Cookies.Append("jwt", newToken.GetString()!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }

            SuccessMessage = "Підписку успішно скасовано.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "❌ Помилка при скасуванні підписки: " + ex.Message;
        }

        return RedirectToPage();
    }
}
