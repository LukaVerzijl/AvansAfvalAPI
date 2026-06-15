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
public class TrashController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly IAuthenticationService _authenticationService;
    
    public TrashController(DatabaseContext context, IAuthenticationService authenticationService)
    {
        _context = context;
        _authenticationService = authenticationService;
    }

    [HttpGet(Name = "GetAllTrash")]
    public async Task<ActionResult<TrashModel>> GetAsync()
    {
        List<TrashModel> trash = await _context.Trash.ToListAsync();
        return Ok(trash);
    }
}