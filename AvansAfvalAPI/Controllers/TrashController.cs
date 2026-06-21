using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AvansAfvalAPI.Database;
using AvansAfvalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvansAfvalAPI.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class TrashController(DatabaseContext context) : ControllerBase
{
    [HttpGet(Name = "GetTrash")]
    public async Task<ActionResult<IEnumerable<Trash>>> GetAsync([FromQuery] DateTime? fromDate, [FromQuery]  DateTime? toDate)
    {
        var query = context.Trash.AsNoTracking();

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CaptureDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CaptureDate <= toDate.Value);
        }

        var trash = await query.ToListAsync();
        return Ok(trash);
    }
    
    [HttpGet("{id}", Name = "GetTrashById")]
    public async Task<ActionResult<Trash>> GetByIdAsync(int id)
    {
        var trash = await context.Trash.FindAsync(id);

        if (trash == null)
        {
            return NotFound();
        }

        return Ok(trash);
    }

    [HttpPost(Name = "CreateTrash")]
    public async Task<ActionResult<Trash>> CreateAsync(CreateTrashRequest request)
    {
        var trash = new Trash
        {
            CaptureDate = request.CaptureDate,
            GarbageType = request.GarbageType,
            Location = request.Location,
            Confidence = request.Confidence,
            ExternalParameters = request.ExternalParameters
        };

        context.Trash.Add(trash);
        await context.SaveChangesAsync();
        return CreatedAtRoute("GetTrashById", new { id = trash.Id }, trash);
    }
}

public class CreateTrashRequest
{
    public DateTime CaptureDate { get; set; }

    [Required]
    public string GarbageType { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    public double Confidence { get; set; }

    [Required]
    public JsonDocument ExternalParameters { get; set; } = null!;
}
