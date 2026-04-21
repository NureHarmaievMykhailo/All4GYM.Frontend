using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public LoginModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    [Required(ErrorMessage = "Email обовʼязковий")]
    [EmailAddress(ErrorMessage = "Невірний формат email")]
    public string Email { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "Пароль обовʼязковий")]
    [MinLength(6, ErrorMessage = "Пароль має бути не менше 6 символів")]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Console.WriteLine("❌ Model validation failed");
            return Page();
        }

        try
        {
            // ✅ ИСПОЛЬЗУЕМ НАШ NAMED CLIENT
            var http = _httpClientFactory.CreateClient("ApiClient");

            var payload = new
            {
                email = Email,
                password = Password
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            Console.WriteLine($"📤 Sending login payload: {jsonPayload}");

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync("api/User/login", content);

            Console.WriteLine($"🔁 Status code: {response.StatusCode}");

            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"🔃 Response body: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                var token = JsonDocument.Parse(responseBody)
                    .RootElement
                    .GetProperty("token")
                    .GetString();

                Console.WriteLine("✅ Login successful. Token received.");

                Response.Cookies.Append("jwt", token!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // 🔥 ВАЖНО для Railway (HTTPS)
                    SameSite = SameSiteMode.None, // 🔥 критично для cross-site
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                return RedirectToPage("/Profile");
            }
            else
            {
                ErrorMessage = "❌ Невірний email або пароль.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Exception during login: {ex.Message}");
            ErrorMessage = "⚠️ Помилка з'єднання або обробки. Спробуйте ще раз.";
            return Page();
        }
    }
}