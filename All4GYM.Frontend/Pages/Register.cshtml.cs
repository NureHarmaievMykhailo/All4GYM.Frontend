using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class RegisterModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RegisterModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    [Required(ErrorMessage = "Імʼя є обовʼязковим")]
    public string FullName { get; set; } = "";

    [BindProperty]
    [EmailAddress(ErrorMessage = "Невірний формат email")]
    [Required(ErrorMessage = "Email є обовʼязковим")]
    public string Email { get; set; } = "";

    [BindProperty]
    [MinLength(6, ErrorMessage = "Пароль має містити мінімум 6 символів")]
    [Required(ErrorMessage = "Пароль є обовʼязковим")]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();
        
        var http = _httpClientFactory.CreateClient("ApiClient");

        var payload = new
        {
            fullName = FullName,
            email = Email,
            password = Password
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await http.PostAsync("api/User/register", content);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToPage("/Login");
        }

        var errorJson = await response.Content.ReadAsStringAsync();

        try
        {
            var error = JsonDocument.Parse(errorJson);
            ErrorMessage = error.RootElement.GetProperty("message").GetString();
        }
        catch
        {
            ErrorMessage = "Помилка під час реєстрації";
        }

        return Page();
    }
}