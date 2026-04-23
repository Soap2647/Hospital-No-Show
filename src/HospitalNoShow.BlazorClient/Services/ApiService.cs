using HospitalNoShow.BlazorClient.Auth;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HospitalNoShow.BlazorClient.Services;

/// <summary>
/// Backend API'ye tüm HTTP çağrılarının geçtiği merkezi servis.
/// JWT token'ı otomatik ekler, hata yönetimini standartlaştırır.
/// </summary>
public class ApiService(HttpClient httpClient, TokenService tokenService)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Her istekte Authorization header'ını token'dan günceller.
    /// </summary>
    private async Task PrepareRequestAsync()
    {
        var token = await tokenService.GetTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    // ── GET ──────────────────────────────────────────────────────────────────

    public async Task<ApiResult<T>> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        await PrepareRequestAsync();
        try
        {
            var response = await httpClient.GetAsync(endpoint, ct);
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.NetworkError(ex.Message);
        }
    }

    // ── POST ─────────────────────────────────────────────────────────────────

    public async Task<ApiResult<T>> PostAsync<T>(string endpoint, object body, CancellationToken ct = default)
    {
        await PrepareRequestAsync();
        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, content, ct);
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.NetworkError(ex.Message);
        }
    }

    public async Task<ApiResult> PostAsync(string endpoint, object body, CancellationToken ct = default)
    {
        await PrepareRequestAsync();
        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, content, ct);
            return await HandleResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult.NetworkError(ex.Message);
        }
    }

    // ── PATCH ────────────────────────────────────────────────────────────────

    public async Task<ApiResult> PatchAsync(string endpoint, object body, CancellationToken ct = default)
    {
        await PrepareRequestAsync();
        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = content };
            var response = await httpClient.SendAsync(request, ct);
            return await HandleResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult.NetworkError(ex.Message);
        }
    }

    // ── DELETE ───────────────────────────────────────────────────────────────

    public async Task<ApiResult> DeleteAsync(string endpoint, object body, CancellationToken ct = default)
    {
        await PrepareRequestAsync();
        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            var request = new HttpRequestMessage(HttpMethod.Delete, endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var response = await httpClient.SendAsync(request, ct);
            return await HandleResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult.NetworkError(ex.Message);
        }
    }

    // ── Response Handlers ────────────────────────────────────────────────────

    private static async Task<ApiResult<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return ApiResult<T>.Success(data!);
        }

        var errorBody = await response.Content.ReadAsStringAsync();
        var errorMsg = TryExtractError(errorBody) ?? $"HTTP {(int)response.StatusCode}";

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => ApiResult<T>.Failure("Oturum süresi doldu. Lütfen tekrar giriş yapın.", 401),
            HttpStatusCode.Forbidden => ApiResult<T>.Failure("Bu işlem için yetkiniz yok.", 403),
            HttpStatusCode.NotFound => ApiResult<T>.Failure("İstenen kayıt bulunamadı.", 404),
            _ => ApiResult<T>.Failure(errorMsg, (int)response.StatusCode)
        };
    }

    private static async Task<ApiResult> HandleResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        var errorBody = await response.Content.ReadAsStringAsync();
        var errorMsg = TryExtractError(errorBody) ?? $"HTTP {(int)response.StatusCode}";

        return ApiResult.Failure(errorMsg, (int)response.StatusCode);
    }

    private static string? TryExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString();
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("errors", out var errors))
                return string.Join(", ", errors.EnumerateArray().Select(e => e.GetString()));
        }
        catch { }
        return body.Length > 200 ? body[..200] : body;
    }
}

// ── Result Types ─────────────────────────────────────────────────────────────

public class ApiResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public string? ErrorMessage { get; private init; }
    public int StatusCode { get; private init; }
    public bool IsUnauthorized => StatusCode == 401;

    public static ApiResult<T> Success(T data) => new() { IsSuccess = true, Data = data, StatusCode = 200 };
    public static ApiResult<T> Failure(string error, int code = 0) => new() { IsSuccess = false, ErrorMessage = error, StatusCode = code };
    public static ApiResult<T> NetworkError(string msg) => new() { IsSuccess = false, ErrorMessage = $"Bağlantı hatası: {msg}", StatusCode = -1 };
}

public class ApiResult
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }
    public int StatusCode { get; private init; }
    public bool IsUnauthorized => StatusCode == 401;

    public static ApiResult Success() => new() { IsSuccess = true, StatusCode = 204 };
    public static ApiResult Failure(string error, int code = 0) => new() { IsSuccess = false, ErrorMessage = error, StatusCode = code };
    public static ApiResult NetworkError(string msg) => new() { IsSuccess = false, ErrorMessage = $"Bağlantı hatası: {msg}", StatusCode = -1 };
}
