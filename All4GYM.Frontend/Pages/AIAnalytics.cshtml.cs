using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using All4GYM.Frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace All4GYM.Frontend.Pages;

public class AIAnalyticsModel : BasePageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AIAnalyticsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public class AIAnalysisResultDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("periodDays")]
        public int PeriodDays { get; set; }

        [JsonPropertyName("overview")]
        public string Overview { get; set; } = string.Empty;

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();

        [JsonPropertyName("trendPrediction")]
        public string TrendPrediction { get; set; } = string.Empty;

        [JsonPropertyName("vectorType")]
        public string VectorType { get; set; } = string.Empty;

        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; }
    }

    public class AIAnalysisRequestDto
    {
        [JsonPropertyName("periodDays")]
        public int PeriodDays { get; set; }

        [JsonPropertyName("vectorType")]
        public string VectorType { get; set; } = string.Empty;
    }

    public class SubmitFeedbackDto
    {
        [JsonPropertyName("aiReviewId")]
        public int AIReviewId { get; set; }

        [JsonPropertyName("isHelpful")]
        public bool IsHelpful { get; set; }
    }

    public List<AIAnalysisResultDto> NutritionHistory { get; set; } = new();
    public List<AIAnalysisResultDto> WorkoutHistory { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt))
            return RedirectToPage("/Login");

        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            NutritionHistory =
                await client.GetFromJsonAsync<List<AIAnalysisResultDto>>("api/AI/history/Nutrition")
                ?? new();

            WorkoutHistory =
                await client.GetFromJsonAsync<List<AIAnalysisResultDto>>("api/AI/history/Workout")
                ?? new();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostGenerateReviewAsync([FromBody] AIAnalysisRequestDto request)
    {
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt))
            return Unauthorized();

        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync("api/AI/review", request);

            if (!response.IsSuccessStatusCode)
                return BadRequest(await response.Content.ReadAsStringAsync());

            var result = await response.Content.ReadFromJsonAsync<AIAnalysisResultDto>();
            return new JsonResult(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostSubmitFeedbackAsync([FromBody] SubmitFeedbackDto request)
    {
        var jwt = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwt))
            return Unauthorized();

        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync("api/AI/feedback", request);

            if (!response.IsSuccessStatusCode)
                return BadRequest();

            return new JsonResult(new { success = true });
        }
        catch
        {
            return BadRequest();
        }
    }
}