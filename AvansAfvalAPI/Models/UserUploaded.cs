using System.Text.Json;

namespace AvansAfvalAPI.Models;

public class UserUploaded
{
    public Guid UploadId { get; set; }
    public string? UserId { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageName { get; set; }
    public string? GarbageType { get; set; }
    public JsonDocument? ExternalParameters { get; set; }
    public double Confidence { get; set; }
}