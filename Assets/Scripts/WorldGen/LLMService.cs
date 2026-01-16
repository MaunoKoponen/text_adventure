using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace WorldGen
{
    /// <summary>
    /// Service for making LLM API calls. Supports OpenAI and Anthropic.
    /// Handles rate limiting, retries, and request queuing.
    /// </summary>
    public class LLMService : MonoBehaviour
    {
        public static LLMService Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private LLMApiConfig config;

        // Request queue for rate limiting
        private Queue<LLMRequest> requestQueue = new Queue<LLMRequest>();
        private bool isProcessing = false;
        private float lastRequestTime = 0f;

        // Events
        public event Action<LLMResponse> OnResponseReceived;
        public event Action<string> OnError;
        public event Action<float> OnProgressUpdated;
        public event Action<string> OnStatusUpdate;

        // API key storage (not serialized for security)
        private string apiKey;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize the service with configuration.
        /// </summary>
        public void Initialize(LLMApiConfig apiConfig, string key)
        {
            config = apiConfig;
            apiKey = key;
            Debug.Log($"LLMService initialized with provider: {config.provider}, model: {config.model}");
        }

        /// <summary>
        /// Set API key (stored in memory only, not serialized).
        /// </summary>
        public void SetApiKey(string key)
        {
            apiKey = key;
        }

        /// <summary>
        /// Queue a request for processing.
        /// </summary>
        public void QueueRequest(LLMRequest request)
        {
            requestQueue.Enqueue(request);
            if (!isProcessing)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        /// <summary>
        /// Send a request and wait for response (async).
        /// </summary>
        public async Task<LLMResponse> SendRequestAsync(string prompt, string systemPrompt = null)
        {
            var tcs = new TaskCompletionSource<LLMResponse>();

            var request = new LLMRequest
            {
                requestId = Guid.NewGuid().ToString(),
                prompt = prompt,
                systemPrompt = systemPrompt,
                callback = response => tcs.SetResult(response)
            };

            QueueRequest(request);

            return await tcs.Task;
        }

        /// <summary>
        /// Process queued requests with rate limiting.
        /// </summary>
        private IEnumerator ProcessQueue()
        {
            isProcessing = true;

            while (requestQueue.Count > 0)
            {
                // Rate limiting
                float timeSinceLastRequest = Time.realtimeSinceStartup - lastRequestTime;
                float delaySeconds = config.requestDelayMs / 1000f;

                if (timeSinceLastRequest < delaySeconds)
                {
                    yield return new WaitForSeconds(delaySeconds - timeSinceLastRequest);
                }

                var request = requestQueue.Dequeue();
                OnStatusUpdate?.Invoke($"Processing request: {request.type}");

                yield return StartCoroutine(SendRequestCoroutine(request));

                lastRequestTime = Time.realtimeSinceStartup;
            }

            isProcessing = false;
        }

        /// <summary>
        /// Send a single request with retry logic.
        /// </summary>
        private IEnumerator SendRequestCoroutine(LLMRequest request)
        {
            LLMResponse response = null;
            int attempts = 0;

            while (attempts < config.maxRetries)
            {
                attempts++;

                if (config.provider.ToLower() == "openai")
                {
                    yield return StartCoroutine(SendOpenAIRequest(request, r => response = r));
                }
                else if (config.provider.ToLower() == "anthropic")
                {
                    yield return StartCoroutine(SendAnthropicRequest(request, r => response = r));
                }
                else
                {
                    response = new LLMResponse
                    {
                        requestId = request.requestId,
                        success = false,
                        error = $"Unknown provider: {config.provider}"
                    };
                    break;
                }

                if (response.success)
                {
                    break;
                }

                // Retry delay
                if (attempts < config.maxRetries)
                {
                    OnStatusUpdate?.Invoke($"Request failed, retrying ({attempts}/{config.maxRetries})...");
                    yield return new WaitForSeconds(config.retryDelayMs / 1000f);
                }
            }

            OnResponseReceived?.Invoke(response);
            request.callback?.Invoke(response);
        }

        /// <summary>
        /// Send request to OpenAI API.
        /// </summary>
        private IEnumerator SendOpenAIRequest(LLMRequest request, Action<LLMResponse> callback)
        {
            string url = "https://api.openai.com/v1/chat/completions";

            var messages = new List<object>();

            if (!string.IsNullOrEmpty(request.systemPrompt))
            {
                messages.Add(new { role = "system", content = request.systemPrompt });
            }

            messages.Add(new { role = "user", content = request.prompt });

            var requestBody = new OpenAIRequest
            {
                model = config.model,
                messages = messages.ToArray(),
                temperature = config.temperature,
                max_tokens = config.maxTokensPerRequest
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            // JsonUtility doesn't handle anonymous objects well, use manual JSON
            jsonBody = BuildOpenAIRequestJson(request, config);

            using (var webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return webRequest.SendWebRequest();

                var response = new LLMResponse { requestId = request.requestId };

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var jsonResponse = JsonUtility.FromJson<OpenAIResponse>(webRequest.downloadHandler.text);
                        if (jsonResponse.choices != null && jsonResponse.choices.Length > 0)
                        {
                            response.content = jsonResponse.choices[0].message.content;
                            response.success = true;
                            response.tokensUsed = jsonResponse.usage?.total_tokens ?? 0;
                        }
                        else
                        {
                            response.success = false;
                            response.error = "No choices in response";
                        }
                    }
                    catch (Exception e)
                    {
                        response.success = false;
                        response.error = $"Parse error: {e.Message}";
                    }
                }
                else
                {
                    response.success = false;
                    response.error = $"{webRequest.responseCode}: {webRequest.error} - {webRequest.downloadHandler.text}";
                    OnError?.Invoke(response.error);
                }

                callback(response);
            }
        }

        /// <summary>
        /// Send request to Anthropic API.
        /// </summary>
        private IEnumerator SendAnthropicRequest(LLMRequest request, Action<LLMResponse> callback)
        {
            string url = "https://api.anthropic.com/v1/messages";

            string jsonBody = BuildAnthropicRequestJson(request, config);

            using (var webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("x-api-key", apiKey);
                webRequest.SetRequestHeader("anthropic-version", "2023-06-01");

                yield return webRequest.SendWebRequest();

                var response = new LLMResponse { requestId = request.requestId };

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var jsonResponse = JsonUtility.FromJson<AnthropicResponse>(webRequest.downloadHandler.text);
                        if (jsonResponse.content != null && jsonResponse.content.Length > 0)
                        {
                            response.content = jsonResponse.content[0].text;
                            response.success = true;
                            response.tokensUsed = (jsonResponse.usage?.input_tokens ?? 0) +
                                                  (jsonResponse.usage?.output_tokens ?? 0);
                        }
                        else
                        {
                            response.success = false;
                            response.error = "No content in response";
                        }
                    }
                    catch (Exception e)
                    {
                        response.success = false;
                        response.error = $"Parse error: {e.Message}";
                    }
                }
                else
                {
                    response.success = false;
                    response.error = $"{webRequest.responseCode}: {webRequest.error} - {webRequest.downloadHandler.text}";
                    OnError?.Invoke(response.error);
                }

                callback(response);
            }
        }

        /// <summary>
        /// Build OpenAI request JSON manually (JsonUtility doesn't handle nested objects well).
        /// </summary>
        private string BuildOpenAIRequestJson(LLMRequest request, LLMApiConfig cfg)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{cfg.model}\",");
            sb.Append($"\"temperature\":{cfg.temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            sb.Append($"\"max_tokens\":{cfg.maxTokensPerRequest},");
            sb.Append("\"messages\":[");

            if (!string.IsNullOrEmpty(request.systemPrompt))
            {
                sb.Append("{\"role\":\"system\",\"content\":");
                sb.Append(JsonEscape(request.systemPrompt));
                sb.Append("},");
            }

            sb.Append("{\"role\":\"user\",\"content\":");
            sb.Append(JsonEscape(request.prompt));
            sb.Append("}");

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Build Anthropic request JSON manually.
        /// </summary>
        private string BuildAnthropicRequestJson(LLMRequest request, LLMApiConfig cfg)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{cfg.model}\",");
            sb.Append($"\"max_tokens\":{cfg.maxTokensPerRequest},");

            if (!string.IsNullOrEmpty(request.systemPrompt))
            {
                sb.Append("\"system\":");
                sb.Append(JsonEscape(request.systemPrompt));
                sb.Append(",");
            }

            sb.Append("\"messages\":[");
            sb.Append("{\"role\":\"user\",\"content\":");
            sb.Append(JsonEscape(request.prompt));
            sb.Append("}");
            sb.Append("]}");

            return sb.ToString();
        }

        /// <summary>
        /// Escape string for JSON.
        /// </summary>
        private string JsonEscape(string str)
        {
            if (string.IsNullOrEmpty(str)) return "\"\"";

            var sb = new System.Text.StringBuilder();
            sb.Append("\"");
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append("\"");
            return sb.ToString();
        }

        /// <summary>
        /// Get queue status.
        /// </summary>
        public int GetQueueCount() => requestQueue.Count;
        public bool IsProcessing => isProcessing;
    }

    /// <summary>
    /// LLM request data.
    /// </summary>
    [Serializable]
    public class LLMRequest
    {
        public string requestId;
        public string prompt;
        public string systemPrompt;
        public RequestType type;
        public Action<LLMResponse> callback;
    }

    public enum RequestType
    {
        ChapterOutline,
        Location,
        Quest,
        Dialogue,
        Enemy,
        Item,
        Map
    }

    /// <summary>
    /// LLM response data.
    /// </summary>
    [Serializable]
    public class LLMResponse
    {
        public string requestId;
        public string content;
        public bool success;
        public string error;
        public int tokensUsed;
    }

    // OpenAI API response structures
    [Serializable]
    public class OpenAIRequest
    {
        public string model;
        public object[] messages;
        public float temperature;
        public int max_tokens;
    }

    [Serializable]
    public class OpenAIResponse
    {
        public OpenAIChoice[] choices;
        public OpenAIUsage usage;
    }

    [Serializable]
    public class OpenAIChoice
    {
        public OpenAIMessage message;
    }

    [Serializable]
    public class OpenAIMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class OpenAIUsage
    {
        public int total_tokens;
    }

    // Anthropic API response structures
    [Serializable]
    public class AnthropicResponse
    {
        public AnthropicContent[] content;
        public AnthropicUsage usage;
    }

    [Serializable]
    public class AnthropicContent
    {
        public string type;
        public string text;
    }

    [Serializable]
    public class AnthropicUsage
    {
        public int input_tokens;
        public int output_tokens;
    }
}
