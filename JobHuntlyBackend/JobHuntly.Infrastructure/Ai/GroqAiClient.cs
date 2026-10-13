using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobHuntly.Application.Ai;
using JobHuntly.Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobHuntly.Infrastructure.Ai;

/// <summary>
/// Calls Groq's OpenAI-compatible chat completions API. The key and the model are read from
/// configuration on every call (Groq:ApiKey, Groq:Model), never captured at startup, never sent to the
/// browser and never logged. Prompts and answers are not logged either: they contain user text.
/// </summary>
public class GroqAiClient(HttpClient http, IConfiguration configuration, ILogger<GroqAiClient> logger) : IAiClient
{
    public const string Endpoint = "https://api.groq.com/openai/v1/chat/completions";
    public const string DefaultModel = "openai/gpt-oss-120b";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["Groq:ApiKey"]);

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var apiKey = configuration["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ExternalServiceException("AI features are not available on this server. You can fill this in manually.", "ai_not_configured");

        // Groq:Endpoint exists so end-to-end tests can point at a local mock instead of the real service.
        var endpoint = configuration["Groq:Endpoint"] is { Length: > 0 } custom ? custom : Endpoint;
        var model = configuration["Groq:Model"] is { Length: > 0 } configured ? configured : DefaultModel;
        var timeoutSeconds = configuration.GetValue("Groq:TimeoutSeconds", 10);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["temperature"] = 0.2,
            ["max_completion_tokens"] = 3000,
            ["response_format"] = new { type = "json_object" }
        };
        // gpt-oss models "think" before answering; a low effort keeps latency and token use small.
        if (model.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase)) payload["reasoning_effort"] = "low";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            using var response = await http.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                // Log the provider's own error text (e.g. "model_decommissioned") but never our key or the prompts.
                logger.LogWarning("Groq returned {Status} for model {Model}: {Error}",
                    (int)response.StatusCode, model, ExtractError(body));
                throw new ExternalServiceException(
                    response.StatusCode == HttpStatusCode.TooManyRequests
                        ? "The AI provider is busy right now. Try again in a minute or fill this in manually."
                        : "The AI provider could not answer right now. You can fill this in manually.",
                    "ai_unavailable");
            }

            using var document = JsonDocument.Parse(body);
            var content = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return content ?? string.Empty;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            if (ct.IsCancellationRequested) throw;
            logger.LogWarning(ex, "Groq call failed ({Type})", ex.GetType().Name);
            throw new ExternalServiceException("The AI provider could not answer right now. You can fill this in manually.", "ai_unavailable");
        }
    }

    private static string ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message)
                ? message.GetString() ?? "unknown"
                : "unknown";
        }
        catch (JsonException)
        {
            return "unreadable body";
        }
    }
}
