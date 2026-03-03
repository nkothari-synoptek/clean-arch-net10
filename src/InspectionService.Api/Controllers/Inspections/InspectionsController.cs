using InspectionService.Application.Inspections.Commands.CreateInspection;
using InspectionService.Application.Inspections.Commands.DeleteInspection;
using InspectionService.Application.Inspections.Commands.UpdateInspection;
using InspectionService.Application.Inspections.DTOs;
using InspectionService.Application.Inspections.Queries.GetInspectionById;
using InspectionService.Application.Inspections.Queries.ListInspections;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InspectionService.Api.Controllers.Inspections;

/// <summary>
/// Controller for managing inspections
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InspectionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InspectionsController> _logger;

    public InspectionsController(IMediator mediator, ILogger<InspectionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new inspection
    /// </summary>
    /// <param name="command">Inspection creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created inspection ID</returns>
    [HttpPost]
    [Authorize(Policy = "CanCreateInspection")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInspection(
        [FromBody] CreateInspectionCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new inspection with title: {Title}", command.Title);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create inspection: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Successfully created inspection with ID: {InspectionId}", result.Value);
        return CreatedAtAction(
            nameof(GetInspectionById),
            new { id = result.Value },
            result.Value);
    }

    /// <summary>
    /// Get an inspection by ID
    /// </summary>
    /// <param name="id">Inspection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inspection details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InspectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInspectionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving inspection with ID: {InspectionId}", id);

        var query = new GetInspectionByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Inspection not found: {InspectionId}", id);
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// List inspections with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="status">Filter by status (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of inspections</returns>
    [HttpGet]
    [ProducesResponseType(typeof(InspectionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListInspections(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Listing inspections - Page: {PageNumber}, Size: {PageSize}, Status: {Status}",
            pageNumber,
            pageSize,
            status);

        var query = new ListInspectionsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to list inspections: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update an existing inspection
    /// </summary>
    /// <param name="id">Inspection ID</param>
    /// <param name="command">Updated inspection details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanCreateInspection")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInspection(
        Guid id,
        [FromBody] UpdateInspectionCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            _logger.LogWarning("ID mismatch: URL ID {UrlId} vs Command ID {CommandId}", id, command.Id);
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
        }

        _logger.LogInformation("Updating inspection with ID: {InspectionId}", id);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update inspection {InspectionId}: {Error}", id, result.Error);

            if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Successfully updated inspection with ID: {InspectionId}", id);
        return NoContent();
    }

    /// <summary>
    /// Delete an inspection
    /// </summary>
    /// <param name="id">Inspection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanCreateInspection")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteInspection(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting inspection with ID: {InspectionId}", id);

        var command = new DeleteInspectionCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete inspection {InspectionId}: {Error}", id, result.Error);
            
            if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });
            
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Successfully deleted inspection with ID: {InspectionId}", id);
        return NoContent();
    }
}
