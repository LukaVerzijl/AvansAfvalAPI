namespace AvansAfvalAPI.Prediction;

public sealed class ImagePredictionOptions
{
    public string Endpoint { get; init; } = "http://localhost:8000/predict";
    public int SignedImageUrlMinutes { get; init; } = 10;

    public static ImagePredictionOptions FromConfiguration(IConfiguration configuration)
    {
        return new ImagePredictionOptions
        {
            Endpoint = configuration["Prediction:Endpoint"] ?? "http://localhost:8000/predict",
            SignedImageUrlMinutes = configuration.GetValue("Prediction:SignedImageUrlMinutes", 10)
        };
    }
}
