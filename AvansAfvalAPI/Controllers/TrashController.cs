using System.Diagnostics;
using AvansAfvalAPI.Database;
using AvansAfvalAPI.Interfaces;
using AvansAfvalAPI.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvansAfvalAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class TrashController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly IAuthenticationService _authenticationService;
    
    public TrashController(DatabaseContext context, IAuthenticationService authenticationService)
    {
        _context = context;
        _authenticationService = authenticationService;
    }

    [HttpGet(Name = "GetTrash")]
    public async Task<ActionResult<IEnumerable<TrashModel>>> GetAsync([FromQuery] DateTime? time1, [FromQuery]  DateTime? time2)
    {
        var query = _context.Trash.AsQueryable();

        if (time1.HasValue)
        {
            query = query.Where(t => t.CaptureDate >= time1.Value);
        }

        if (time2.HasValue)
        {
            query = query.Where(t => t.CaptureDate <= time2.Value);
        }

        var trash = await query.ToListAsync();
        return Ok(trash);
    }
    
    [HttpGet("{id}", Name = "GetTrashById")]
    public async Task<ActionResult<TrashModel>> GetByIdAsync(int id)
    {
        var trash = await _context.Trash.FindAsync(id);

        if (trash == null)
        {
            return NotFound();
        }

        return Ok(trash);
    }

    [HttpPost(Name = "CreateTrash")]
    public async Task<ActionResult<TrashModel>> CreateAsync(TrashModel trash)
    {
        _context.Trash.Add(trash);
        await _context.SaveChangesAsync();
        return CreatedAtRoute("GetTrashById", new { id = trash.Id }, trash);
    }
}