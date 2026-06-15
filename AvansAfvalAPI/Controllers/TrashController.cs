using AvansAfvalAPI.Database;
using AvansAfvalAPI.Interfaces;
using AvansAfvalAPI.models;
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
    public async Task<ActionResult<IEnumerable<TrashModel>>> GetAsync([FromQuery] DateTime? fromDate, [FromQuery]  DateTime? toDate)
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
    public async Task<ActionResult<TrashModel>> GetByIdAsync(int id)
    {
        var trash = await context.Trash.FindAsync(id);

        if (trash == null)
        {
            return NotFound();
        }

        return Ok(trash);
    }

    [HttpPost(Name = "CreateTrash")]
    public async Task<ActionResult<TrashModel>> CreateAsync(TrashModel trash)
    {
        context.Trash.Add(trash);
        await context.SaveChangesAsync();
        return CreatedAtRoute("GetTrashById", new { id = trash.Id }, trash);
    }
}