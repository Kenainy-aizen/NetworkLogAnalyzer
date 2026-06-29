using Microsoft.AspNetCore.Mvc;
using Storage.Models;
using Storage.Repositories;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventRepository _repo;

    public EventsController(IEventRepository repo)
    {
        _repo = repo;
    }

    // GET /api/events
    // GET /api/events?severity=CRITICAL
    // GET /api/events?sourceIp=192.168.1.5
    // GET /api/events?limit=50
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? severity,
        [FromQuery] string? sourceIp,
        [FromQuery] int limit = 100)
    {
        var events = await _repo.GetAllAsync(severity, sourceIp, limit);
        return Ok(events);
    }

    // GET /api/events/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _repo.GetByIdAsync(id);
        if (ev is null) return NotFound();
        return Ok(ev);
    }

    // GET /api/events/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _repo.GetStatsAsync();
        return Ok(stats);
    }

    // POST /api/events  (pour tester manuellement)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NetworkEvent networkEvent)
    {
        await _repo.AddAsync(networkEvent);
        return CreatedAtAction(nameof(GetById), new { id = networkEvent.Id }, networkEvent);
    }
}
