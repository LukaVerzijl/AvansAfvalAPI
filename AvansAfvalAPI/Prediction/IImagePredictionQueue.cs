namespace AvansAfvalAPI.Prediction;

public interface IImagePredictionQueue
{
    void Enqueue(Guid uploadId);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}
