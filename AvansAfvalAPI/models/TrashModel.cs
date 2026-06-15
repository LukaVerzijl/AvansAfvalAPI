using System.ComponentModel.DataAnnotations;

namespace AvansAfvalAPI.models;

public class TrashModel
{
    public int Id { get; set; }
    public DateTime CaptureDate { get; set; }
    public string? GarbageType { get; set; }
    public string? Location { get; set; }
    [Range(0, 1, ErrorMessage = "Confidence must be between 0 and 1")]
    public double Confidence { get; set; }
    public Dictionary<string, object>? ExternalParameters { get; set; }
}