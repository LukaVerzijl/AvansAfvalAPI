namespace AvansAfvalAPI.Storage;

public sealed class S3StorageOptions
{
    public string ServiceUrl { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Region { get; init; } = "us-east-1";
    public string? PublicBaseUrl { get; init; }
    public string Prefix { get; init; } = "user-uploads";
    public bool ForcePathStyle { get; init; } = true;
    public bool UsePublicReadAcl { get; init; }

    public static S3StorageOptions FromConfiguration(IConfiguration configuration)
    {
        return new S3StorageOptions
        {
            ServiceUrl = FirstValue(
                configuration,
                "S3:ServiceUrl",
                "S3:Endpoint",
                "AWS_ENDPOINT_URL_S3",
                "AWS_ENDPOINT_URL",
                "BUCKET_ENDPOINT_URL",
                "S3_ENDPOINT"),
            BucketName = FirstValue(
                configuration,
                "S3:BucketName",
                "AWS_BUCKET_NAME",
                "AWS_S3_BUCKET",
                "S3_BUCKET",
                "BUCKET_NAME"),
            AccessKey = FirstValue(
                configuration,
                "S3:AccessKey",
                "AWS_ACCESS_KEY_ID",
                "S3_ACCESS_KEY_ID",
                "BUCKET_ACCESS_KEY_ID"),
            SecretKey = FirstValue(
                configuration,
                "S3:SecretKey",
                "AWS_SECRET_ACCESS_KEY",
                "S3_SECRET_ACCESS_KEY",
                "BUCKET_SECRET_ACCESS_KEY"),
            Region = FirstValue(
                configuration,
                ["S3:Region", "AWS_REGION", "AWS_DEFAULT_REGION", "BUCKET_REGION", "S3_REGION"],
                fallback: "us-east-1"),
            PublicBaseUrl = FirstValueOrNull(
                configuration,
                "S3:PublicBaseUrl",
                "S3_PUBLIC_URL",
                "BUCKET_PUBLIC_URL"),
            Prefix = FirstValue(
                configuration,
                ["S3:Prefix", "BUCKET_PREFIX"],
                fallback: "user-uploads"),
            ForcePathStyle = configuration.GetValue("S3:ForcePathStyle", true),
            UsePublicReadAcl = configuration.GetValue("S3:UsePublicReadAcl", false)
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServiceUrl))
            throw new InvalidOperationException("S3 storage is missing ServiceUrl. Configure S3:ServiceUrl or AWS_ENDPOINT_URL.");

        if (string.IsNullOrWhiteSpace(BucketName))
            throw new InvalidOperationException("S3 storage is missing BucketName. Configure S3:BucketName or AWS_BUCKET_NAME.");

        if (string.IsNullOrWhiteSpace(AccessKey))
            throw new InvalidOperationException("S3 storage is missing AccessKey. Configure S3:AccessKey or AWS_ACCESS_KEY_ID.");

        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("S3 storage is missing SecretKey. Configure S3:SecretKey or AWS_SECRET_ACCESS_KEY.");
    }

    private static string FirstValue(IConfiguration configuration, params string[] keys)
    {
        return FirstValue(configuration, keys, fallback: string.Empty);
    }

    private static string FirstValue(IConfiguration configuration, string[] keys, string fallback)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return fallback;
    }

    private static string? FirstValueOrNull(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
