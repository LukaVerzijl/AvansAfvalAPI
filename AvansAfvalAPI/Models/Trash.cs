using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AvansAfvalAPI.Models;

public class Trash
{
    public int Id { get; set; }
    public DateTime CaptureDate { get; set; }
    [Required]
    public string GarbageType { get; set; } = string.Empty;
    [Required]
    public string Location { get; set; } = string.Empty;
    public double Confidence { get; set; }
    [Column(TypeName = "jsonb")]
    public JsonDocument ExternalParameters { get; set; } = null!;
}
