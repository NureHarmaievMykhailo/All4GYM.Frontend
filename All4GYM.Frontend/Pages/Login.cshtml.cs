using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class LoginModel : PageModel
{
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
        // 🔎 Перевірка валідації
        if (!ModelState.IsValid)
        {
            Console.WriteLine("❌ Model validation failed");
            return Page();
        }

        try
        {
            using var http = new HttpClient();
            http.BaseAddress = new Uri("http://localhost:5092/");

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
                var token = JsonDocument.Parse(responseBody).RootElement.GetProperty("token").GetString();

                Console.WriteLine("✅ Login successful. Token received.");

                Response.Cookies.Append("jwt", token!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // для dev
                    SameSite = SameSiteMode.Strict,
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
