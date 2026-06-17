using System.Threading.Channels;

namespace AvansAfvalAPI.Prediction;

public sealed class ImagePredictionQueue : IImagePredictionQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid uploadId)
    {
        _queue.Writer.TryWrite(uploadId);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }
}
