using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using All4GYM.Frontend.Helpers;

namespace All4GYM.Frontend.Pages;

public class SubscriptionsModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SubscriptionsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string GetTierDescription(string tier) => tier switch
    {
        "Basic" => "Доступ до базових функцій платформи",
        "Pro" => "Розширені функції, відео, харчування",
        "Premium" => "Все включено + персональні консультації",
        _ => ""
    };

    public async Task<IActionResult> OnPostSubscribeAsync(string tier)
    {
        Console.WriteLine($"📦 Обрано підписку: {tier}");

        if (string.IsNullOrWhiteSpace(tier))
        {
            ModelState.AddModelError(string.Empty, "Не обрано рівень підписки.");
            return Page();
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/"); // Замінити при деплої
        var jwt = Request.Cookies["jwt"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.PostAsync($"api/Checkout/{tier}", null);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CheckoutResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!string.IsNullOrWhiteSpace(result?.Url))
            {
                return Redirect(result.Url);
            }

            ModelState.AddModelError(string.Empty, "❌ Не вдалося отримати URL для оплати.");
            return Page();
        }

        var errorBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ Stripe error: {errorBody}");
        ModelState.AddModelError(string.Empty, $"❌ Помилка оформлення підписки: {errorBody}");
        return Page();
    }
    
    public async Task<IActionResult> OnPostCancelSubscriptionAsync()
    {
        var jwt = Request.Cookies["jwt"];
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5092/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.PostAsync("api/Subscription/cancel", null);
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Підписку скасовано.";
            return RedirectToPage();
        }

        var error = await response.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"❌ Помилка: {error}");
        return Page();
    }


    public class CheckoutResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
