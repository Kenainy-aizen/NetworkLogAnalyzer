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

    // GET /api/events?search=fakeuser&severity=WARNING&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? severity,
        [FromQuery] string? sourceIp,
        [FromQuery] string? search,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _repo.GetAllAsync(severity, sourceIp, search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _repo.GetByIdAsync(id);
        if (ev is null) return NotFound();
        return Ok(ev);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _repo.GetStatsAsync();
        return Ok(stats);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NetworkEvent networkEvent)
    {
        await _repo.AddAsync(networkEvent);
        return CreatedAtAction(nameof(GetById), new { id = networkEvent.Id }, networkEvent);
    }
}
