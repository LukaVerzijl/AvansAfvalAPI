namespace AvansAfvalAPI.Storage;

public interface IObjectStorageService
{
    Task<StoredObject> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);

    string CreateReadUrl(string objectKeyOrUrl, TimeSpan expiresIn);

    Task DeleteAsync(string objectKeyOrUrl, CancellationToken cancellationToken);
}

public sealed record StoredObject(string Key, string Url);
