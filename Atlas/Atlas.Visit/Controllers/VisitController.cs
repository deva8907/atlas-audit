using Atlas.Visit.Models;
using Atlas.Visit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Atlas.Visit.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitController : ControllerBase
{
    private readonly IVisitService _visitService;
    private readonly ILogger<VisitController> _logger;

    public VisitController(IVisitService visitService, ILogger<VisitController> logger)
    {
        _visitService = visitService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Models.Visit>>> GetVisits()
    {
        try
        {
            var visits = await _visitService.GetAllVisitsAsync();
            return Ok(visits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving visits");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Models.Visit>> GetVisit(int id)
    {
        try
        {
            var visit = await _visitService.GetVisitByIdAsync(id);
            if (visit == null)
            {
                return NotFound();
            }

            return Ok(visit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving visit with ID {VisitId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<IEnumerable<Models.Visit>>> GetVisitsByPatient(int patientId)
    {
        try
        {
            var visits = await _visitService.GetVisitsByPatientIdAsync(patientId);
            return Ok(visits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving visits for patient {PatientId}", patientId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Models.Visit>> CreateVisit([FromBody] Models.Visit visit)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdVisit = await _visitService.CreateVisitAsync(visit);
            return CreatedAtAction(nameof(GetVisit), new { id = createdVisit.Id }, createdVisit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating visit");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVisit(int id, [FromBody] Models.Visit visit)
    {
        try
        {
            if (id != visit.Id)
            {
                return BadRequest("ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedVisit = await _visitService.UpdateVisitAsync(visit);
            return Ok(updatedVisit);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Visit with ID {VisitId} not found for update", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating visit with ID {VisitId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVisit(int id)
    {
        try
        {
            await _visitService.DeleteVisitAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Visit with ID {VisitId} not found for deletion", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visit with ID {VisitId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
