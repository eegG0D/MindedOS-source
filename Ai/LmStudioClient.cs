using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MindedOS.Ai;

/// <summary>
/// Minimal client for LM Studio's OpenAI-compatible local server: discovers the
/// loaded model from /v1/models and runs a chat completion against /v1/chat/completions.
/// </summary>
public sealed class LmStudioClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public LmStudioClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) }; // generation can be slow
    }

    /// <summary>The first model id reported by the server, or null if none/unreachable.</summary>
    public async Task<string?> GetFirstModelAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/v1/models", ct);
        resp.EnsureSuccessStatusCode();
        var models = await resp.Content.ReadFromJsonAsync<ModelsResponse>(cancellationToken: ct);
        return models?.Data?.FirstOrDefault()?.Id;
    }

    /// <summary>Run a chat completion and return the assistant's message content.</summary>
    public async Task<string> CompleteAsync(string model, string system, string user, CancellationToken ct = default)
    {
        var request = new ChatRequest
        {
            Model = model,
            Temperature = 0.7,
            MaxTokens = 4096,
            Stream = false,
            Messages = new[]
            {
                new ChatMessage { Role = "system", Content = system },
                new ChatMessage { Role = "user", Content = user },
            },
        };

        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/v1/chat/completions", request, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
        return body?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
    }

    /// <summary>
    /// Run a chat completion that includes an image (OpenAI-compatible multimodal
    /// content). Requires a vision-capable model loaded in LM Studio. The image is
    /// sent inline as a base64 data URI.
    /// </summary>
    public async Task<string> CompleteWithImageAsync(string model, string system, string user,
        byte[] imageBytes, string mimeType = "image/png", CancellationToken ct = default)
    {
        var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
        var request = new
        {
            model,
            temperature = 0.4,
            max_tokens = 4096,
            stream = false,
            messages = new object[]
            {
                new { role = "system", content = system },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = user },
                        new { type = "image_url", image_url = new { url = dataUri } },
                    },
                },
            },
        };

        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/v1/chat/completions", request, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
        return body?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
    }

    public void Dispose() => _http.Dispose();

    // ---- DTOs -------------------------------------------------------------
    private sealed class ModelsResponse
    {
        [JsonPropertyName("data")] public List<ModelEntry>? Data { get; set; }
    }
    private sealed class ModelEntry
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }
    private sealed class ChatRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = "";
        [JsonPropertyName("messages")] public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();
        [JsonPropertyName("temperature")] public double Temperature { get; set; }
        [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; }
        [JsonPropertyName("stream")] public bool Stream { get; set; }
    }
    private sealed class ChatMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }
    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
    }
    private sealed class Choice
    {
        [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
    }
}
