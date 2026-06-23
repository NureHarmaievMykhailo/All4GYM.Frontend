using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

[IgnoreAntiforgeryToken]
public class AIAnalyticsModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AIAnalyticsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // Модели ответов от API
    public class AIReviewDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("overview")] public string Overview { get; set; } = string.Empty;
        [JsonPropertyName("recommendations")] public List<string> Recommendations { get; set; } = new();
        [JsonPropertyName("trendPrediction")] public string TrendPrediction { get; set; } = string.Empty;
        [JsonPropertyName("vectorType")] public string VectorType { get; set; } = string.Empty;
        [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    }

    public class AIAnalysisRequest
    {
        public int Days { get; set; }
        public string VectorType { get; set; } = string.Empty; // "Nutrition" або "Workout"
    }

    public class FeedbackRequest
    {
        public int ReviewId { get; set; }
        public bool IsPositive { get; set; }
    }

    // Списки для рендеринга истории в левой панели
    public List<AIReviewDto> NutritionHistory { get; set; } = new();
    public List<AIReviewDto> WorkoutHistory { get; set; } = new();
    
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt)) return RedirectToPage("/Login");

        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            // Вытягиваем историю по обоим направлениям
            NutritionHistory = await client.GetFromJsonAsync<List<AIReviewDto>>("api/AI/history/Nutrition") ?? new();
            WorkoutHistory = await client.GetFromJsonAsync<List<AIReviewDto>>("api/AI/history/Workout") ?? new();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не вдалося завантажити історію аналітики: " + ex.Message;
        }

        return Page();
    }

    /// <summary>
    /// AJAX-обработчик для генерации отчета
    /// </summary>
    public async Task<IActionResult> OnPostGenerateReviewAsync([FromBody] AIAnalysisRequest request)
    {
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt)) return Unauthorized();

        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync("api/AI/review", request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return BadRequest(new { message = $"API Error: {err}" });
            }

            var result = await response.Content.ReadFromJsonAsync<AIReviewDto>();
            return new JsonResult(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// AJAX-обработчик для фидбека (лайк/дизлайк)
    /// </summary>
    public async Task<IActionResult> OnPostSubmitFeedbackAsync([FromBody] FeedbackRequest request)
    {
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt)) return Unauthorized();

        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync("api/AI/feedback", request);
            if (!response.IsSuccessStatusCode) return BadRequest();

            return new JsonResult(new { success = true });
        }
        catch
        {
            return BadRequest();
        }
    }
}