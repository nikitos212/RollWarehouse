using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using RollWarehouse.Application.Services;
using RollWarehouse.Application.Abstractions.Ports;
using RollWarehouse.Presentation.Http.Models;

namespace RollWarehouse.Presentation.Http.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RollsController : ControllerBase
    {
        private readonly RollService _service;
        private readonly IRollRepository _repo;
        public RollsController(RollService service, IRollRepository repo) { _service = service; _repo = repo; }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRollRequest req)
        {
            if (req == null) return BadRequest(new { error = "Invalid body" });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var created = await _service.AddRollAsync(req.Length!.Value, req.Weight!.Value);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return NotFound(new { error = "Roll not found" });
            return Ok(r);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteRollAsync(id);
            if (deleted == null) return NotFound(new { error = "Roll not found" });
            return Ok(deleted);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] QueryRequest q)
        {
            var filter = new RollFilter(q.IdMin, q.IdMax, q.WeightMin, q.WeightMax, q.LengthMin, q.LengthMax, q.DateAddedFrom, q.DateAddedTo, q.DateRemovedFrom, q.DateRemovedTo);
            var list = await _service.ListFilteredAsync(filter);
            return Ok(list);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> Statistics([FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] bool includeDayExtrema = false)
        {
            var stat = await _service.GetStatisticsAsync(start, end);
            if (includeDayExtrema)
            {
                var ext = await _service.GetPeriodDayExtremaAsync(start, end);
                return Ok(new { stat, ext });
            }
            return Ok(stat);
        }
    }
}
