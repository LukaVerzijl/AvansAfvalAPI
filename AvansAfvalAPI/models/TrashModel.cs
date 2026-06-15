using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AvansAfvalAPI.models;

public class TrashModel
{
    public int Id { get; set; }
    public DateTime CaptureDate { get; set; }
    public String? GarbageType { get; set; }
    public String? Location { get; set; }
    public Double Confidence { get; set; }
    public Dictionary<string, object>? ExternalParameters { get; set; }
}