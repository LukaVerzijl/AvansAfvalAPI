using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AvansAfvalAPI.Database;
using AvansAfvalAPI.Storage;
using Microsoft.EntityFrameworkCore;

namespace AvansAfvalAPI.Prediction;

public sealed class ImagePredictionWorker(
    IImagePredictionQueue queue,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ImagePredictionOptions options,
    ILogger<ImagePredictionWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var uploadId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await PredictAsync(uploadId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Prediction failed for upload {UploadId}", uploadId);
            }
        }
    }

    private async Task PredictAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var objectStorageService = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

        var upload = await context.UserUploaded
            .FirstOrDefaultAsync(userUpload => userUpload.UploadId == uploadId, cancellationToken);

        if (upload is null)
        {
            logger.LogWarning("Prediction skipped because upload {UploadId} was not found", uploadId);
            return;
        }

        if (string.IsNullOrWhiteSpace(upload.ImageUrl))
        {
            logger.LogWarning("Prediction skipped because upload {UploadId} has no image URL", uploadId);
            return;
        }

        var signedImageUrl = objectStorageService.CreateReadUrl(
            upload.ImageUrl,
            TimeSpan.FromMinutes(options.SignedImageUrlMinutes));

        var httpClient = httpClientFactory.CreateClient("PredictionApi");
        using var response = await httpClient.PostAsync(
            BuildPredictionUrl(signedImageUrl),
            content: null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Prediction API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {errorBody}");
        }

        var prediction = await response.Content.ReadFromJsonAsync<PredictionResponse>(JsonOptions, cancellationToken);
        if (prediction is null)
        {
            logger.LogWarning("Prediction API returned an empty response for upload {UploadId}", uploadId);
            return;
        }

        upload.GarbageType = prediction.Label;
        upload.Confidence = prediction.Confidence;
        upload.ExternalParameters = JsonSerializer.SerializeToDocument(prediction, JsonOptions);

        await context.SaveChangesAsync(cancellationToken);
    }

    private string BuildPredictionUrl(string signedImageUrl)
    {
        var separator = options.Endpoint.Contains('?') ? '&' : '?';
        return $"{options.Endpoint}{separator}image_url={WebUtility.UrlEncode(signedImageUrl)}";
    }

    private sealed record PredictionResponse(
        string Label,
        double Confidence,
        [property: JsonPropertyName("nothing_found")] bool NothingFound,
        double Threshold,
        Dictionary<string, double> Scores);
}
