using System.Globalization;
using Amazon.S3;
using Amazon.S3.Model;

namespace AvansAfvalAPI.Storage;

public sealed class S3ObjectStorageService(IAmazonS3 s3Client, S3StorageOptions options) : IObjectStorageService
{
    public async Task<StoredObject> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        options.Validate();

        var objectKey = CreateObjectKey(fileName);
        var request = new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType
        };

        if (options.UsePublicReadAcl)
            request.CannedACL = S3CannedACL.PublicRead;

        await s3Client.PutObjectAsync(request, cancellationToken);

        return new StoredObject(objectKey, BuildPublicUrl(objectKey));
    }

    public string CreateReadUrl(string objectKeyOrUrl, TimeSpan expiresIn)
    {
        options.Validate();

        var objectKey = ExtractObjectKey(objectKeyOrUrl);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn)
        };

        return s3Client.GetPreSignedURL(request);
    }

    private string CreateObjectKey(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        var prefix = options.Prefix.Trim('/');
        var datePath = DateTimeOffset.UtcNow.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        var generatedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        return string.IsNullOrWhiteSpace(prefix)
            ? $"{datePath}/{generatedName}"
            : $"{prefix}/{datePath}/{generatedName}";
    }

    private string BuildPublicUrl(string objectKey)
    {
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
            return $"{options.PublicBaseUrl.TrimEnd('/')}/{EscapePath(objectKey)}";

        var endpoint = options.ServiceUrl.TrimEnd('/');
        var bucket = Uri.EscapeDataString(options.BucketName);
        return options.ForcePathStyle
            ? $"{endpoint}/{bucket}/{EscapePath(objectKey)}"
            : $"{new Uri(endpoint).Scheme}://{options.BucketName}.{new Uri(endpoint).Authority}/{EscapePath(objectKey)}";
    }

    private string ExtractObjectKey(string objectKeyOrUrl)
    {
        if (!Uri.TryCreate(objectKeyOrUrl, UriKind.Absolute, out var uri))
            return objectKeyOrUrl.TrimStart('/');

        var path = uri.AbsolutePath.TrimStart('/');
        var bucketPrefix = $"{options.BucketName.Trim('/')}/";

        if (path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
            path = path[bucketPrefix.Length..];

        return string.Join(
            '/',
            path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString));
    }

    private static string EscapePath(string path)
    {
        return string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
    }
}
