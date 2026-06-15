using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AvansAfvalAPI.models;

public class TrashModel
{
    public Guid Id { get; set; }
    public DateTime CaptureDate { get; set; }
    public String? GarbageType { get; set; }
    public String? Location { get; set; }
    public Double Confidence { get; set; }
    public JsonContent? ExternalParameters { get; set; }
}