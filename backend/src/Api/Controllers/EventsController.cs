using Microsoft.AspNetCore.Mvc;
using Storage;
using Storage.Models;
using Storage.Repositories;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventRepository _repo;
    private readonly IAnalysisTrigger _analysisTrigger;

    public EventsController(IEventRepository repo, IAnalysisTrigger analysisTrigger)
    {
        _repo = repo;
        _analysisTrigger = analysisTrigger;
    }

    // GET /api/events?severity=WARNING&sourceIp=1.2.3.4&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? severity,
        [FromQuery] string? sourceIp,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _repo.GetAllAsync(severity, sourceIp, page, pageSize);
        return Ok(result);
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

    // POST /api/events
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NetworkEvent networkEvent)
    {
        await _repo.AddAsync(networkEvent);
        return CreatedAtAction(nameof(GetById), new { id = networkEvent.Id }, networkEvent);
    }
}
